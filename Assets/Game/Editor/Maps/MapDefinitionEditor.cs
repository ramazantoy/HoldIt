using System;
using System.Collections.Generic;
using System.Globalization;
using AF.Core.Editor;
using HoldIt.Board;
using HoldIt.Editor.Simulation;
using HoldIt.Levels;
using UnityEditor;
using UnityEngine;

namespace HoldIt.Editor.Maps
{
    /// <summary>
    /// The map's Inspector: paint it tile by tile (left button paints the brush, right button picks the tile under it),
    /// resize it, copy or paste its rows as text, and see at once what "AF/Validate All" would say and how long its walks
    /// are. It shows the levels played on it, simulates the first of them (<see cref="Difficulty"/>), and can fill it from
    /// a band's recipe. Every change marks the map hand-made, so "HoldIt/Maps/Generate" keeps it.
    /// </summary>
    [CustomEditor(typeof(MapDefinition))]
    public sealed class MapDefinitionEditor : UnityEditor.Editor
    {
        private const float MaxTileSize = 26f;
        private const float MinTileSize = 10f;

        private static readonly Dictionary<char, Color> _colors = new Dictionary<char, Color>
        {
            [MapSymbols.Floor] = new Color32(0xd9, 0xdd, 0xe3, 0xff),
            [MapSymbols.Lobby] = new Color32(0xf4, 0xe3, 0xa1, 0xff),
            [MapSymbols.Entrance] = new Color32(0x7f, 0xd1, 0x8b, 0xff),
            [MapSymbols.Wall] = new Color32(0x4a, 0x55, 0x63, 0xff),
            [MapSymbols.Narrow] = new Color32(0xf0, 0xa0, 0x4b, 0xff),
            [MapSymbols.MaleBuilding] = new Color32(0x8f, 0xb9, 0xee, 0xff),
            [MapSymbols.MaleDoor] = new Color32(0x2f, 0x7d, 0xe1, 0xff),
            [MapSymbols.FemaleBuilding] = new Color32(0xf3, 0xa5, 0xc7, 0xff),
            [MapSymbols.FemaleDoor] = new Color32(0xf0, 0x60, 0x9e, 0xff),
        };

        private static readonly Dictionary<char, string> _names = new Dictionary<char, string>
        {
            [MapSymbols.Floor] = "Floor",
            [MapSymbols.Lobby] = "Lobby",
            [MapSymbols.Entrance] = "Entrance",
            [MapSymbols.Wall] = "Wall",
            [MapSymbols.Narrow] = "Narrow",
            [MapSymbols.MaleBuilding] = "Men's",
            [MapSymbols.MaleDoor] = "Men's door",
            [MapSymbols.FemaleBuilding] = "Women's",
            [MapSymbols.FemaleDoor] = "Women's door",
        };

        private static char _brush = MapSymbols.Wall;

        private SerializedProperty _rows;
        private SerializedProperty _stalls;
        private SerializedProperty _handMade;
        private int _newCols;
        private int _newRows;
        private int _band;
        private int _seed = 1;
        private string _checkedRows;
        private ValidationReport _report;
        private string _metrics;
        private string _note;
        private readonly List<LevelDefinition> _users = new List<LevelDefinition>();
        private GUIStyle _tileLabel;

        private void OnEnable()
        {
            _rows = serializedObject.FindProperty("_rows");
            _stalls = serializedObject.FindProperty("_stallsPerRestroom");
            _handMade = serializedObject.FindProperty("_handMade");
            var rows = ReadRows();
            _newRows = Mathf.Max(1, rows.Length);
            _newCols = Mathf.Max(1, rows.Length > 0 ? rows[0].Length : 10);
            FindUsers();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var rows = ReadRows();

            DrawPalette();
            DrawGrid(rows);
            DrawSize(rows);
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(_stalls);
            EditorGUILayout.PropertyField(_handMade);
            DrawClipboard(rows);
            EditorGUILayout.Space();
            DrawCheck(rows);
            EditorGUILayout.Space();
            DrawLevels(rows);
            EditorGUILayout.Space();
            DrawGenerate();
            if (!string.IsNullOrEmpty(_note))
                EditorGUILayout.HelpBox(_note, MessageType.None);

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawPalette()
        {
            EditorGUILayout.LabelField("Brush (left paints, right picks)", EditorStyles.boldLabel);
            const int perRow = 5;
            for (var start = 0; start < MapSymbols.All.Length; start += perRow)
            {
                EditorGUILayout.BeginHorizontal();
                for (var i = start; i < Mathf.Min(start + perRow, MapSymbols.All.Length); i++)
                {
                    var symbol = MapSymbols.All[i];
                    var previous = GUI.backgroundColor;
                    GUI.backgroundColor = _colors[symbol];
                    var label = $"{symbol} {_names[symbol]}";
                    if (GUILayout.Toggle(_brush == symbol, label, EditorStyles.miniButton))
                        _brush = symbol;
                    GUI.backgroundColor = previous;
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawGrid(string[] rows)
        {
            if (rows.Length == 0 || rows[0].Length == 0)
            {
                EditorGUILayout.HelpBox("The map is empty: give it a size below to start.", MessageType.Info);
                return;
            }

            var cols = rows[0].Length;
            var width = EditorGUIUtility.currentViewWidth - 40f;
            var tile = Mathf.Clamp(Mathf.Floor(width / cols), MinTileSize, MaxTileSize);
            var area = GUILayoutUtility.GetRect(cols * tile, rows.Length * tile, GUILayout.ExpandWidth(false));
            _tileLabel ??= new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleCenter };

            for (var row = 0; row < rows.Length; row++)
            {
                for (var col = 0; col < cols; col++)
                {
                    var symbol = col < rows[row].Length ? rows[row][col] : ' ';
                    var cell = new Rect(area.x + col * tile, area.y + row * tile, tile - 1f, tile - 1f);
                    EditorGUI.DrawRect(cell, _colors.TryGetValue(symbol, out var color) ? color : Color.magenta);
                    if (Event.current.type == EventType.Repaint && tile >= 14f)
                        GUI.Label(cell, symbol.ToString(), _tileLabel);
                }
            }

            HandlePointer(area, tile, rows);
        }

        private void HandlePointer(Rect area, float tile, string[] rows)
        {
            var current = Event.current;
            if ((current.type != EventType.MouseDown && current.type != EventType.MouseDrag) || !area.Contains(current.mousePosition))
                return;

            var cell = new Cell((int)((current.mousePosition.x - area.x) / tile), (int)((current.mousePosition.y - area.y) / tile));
            if (cell.Row >= rows.Length || cell.Col >= rows[cell.Row].Length)
                return;

            if (current.button == 1 && current.type == EventType.MouseDown)
            {
                _brush = rows[cell.Row][cell.Col];
                current.Use();
                Repaint();
            }
            else if (current.button == 0)
            {
                if (rows[cell.Row][cell.Col] != _brush)
                    WriteRows(MapRows.Set(rows, cell, _brush));
                current.Use();
            }
        }

        private void DrawSize(string[] rows)
        {
            EditorGUILayout.BeginHorizontal();
            _newCols = Mathf.Max(1, EditorGUILayout.IntField("Columns × rows", _newCols));
            _newRows = Mathf.Max(1, EditorGUILayout.IntField(_newRows, GUILayout.Width(50f)));
            var size = rows.Length > 0 ? $"{rows[0].Length} × {rows.Length}" : "empty";
            using (new EditorGUI.DisabledScope(rows.Length == _newRows && rows.Length > 0 && rows[0].Length == _newCols))
            {
                if (GUILayout.Button($"Resize ({size})", GUILayout.Width(120f)))
                    WriteRows(MapRows.Resize(rows, _newCols, _newRows));
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawClipboard(string[] rows)
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Copy rows"))
                EditorGUIUtility.systemCopyBuffer = string.Join("\n", rows);
            if (GUILayout.Button("Paste rows"))
            {
                var pasted = MapRows.Parse(EditorGUIUtility.systemCopyBuffer);
                if (pasted.Length > 0)
                {
                    WriteRows(pasted);
                    _newRows = pasted.Length;
                    _newCols = pasted[0].Length;
                }
                else
                {
                    _note = "The clipboard holds no rows.";
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawCheck(string[] rows)
        {
            var joined = string.Join("\n", rows);
            if (_report == null || joined != _checkedRows || GUI.changed)
                Check(joined);

            EditorGUILayout.LabelField("Check", EditorStyles.boldLabel);
            if (_report.Issues.Count == 0)
                EditorGUILayout.HelpBox("Playable: AF/Validate All has nothing to say.", MessageType.Info);
            foreach (var issue in _report.Issues)
            {
                EditorGUILayout.HelpBox(issue.Message,
                    issue.Severity == ValidationSeverity.Error ? MessageType.Error : MessageType.Warning);
            }
            if (!string.IsNullOrEmpty(_metrics))
                EditorGUILayout.LabelField(_metrics, EditorStyles.wordWrappedMiniLabel);
        }

        private void Check(string joined)
        {
            _checkedRows = joined;
            _report = new ValidationReport();
            _metrics = null;
            var map = (MapDefinition)target;
            MapValidator.Check(map, MapValidator.ProjectGeometry(), MapValidator.ProjectLimits(), _report);
            if (_report.ErrorCount == 0 && map.Rows.Count > 0)
                _metrics = MapMetrics.Of(MapParser.Parse(map.Rows)).ToString();
        }

        private void DrawLevels(string[] rows)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Played in", EditorStyles.boldLabel);
            if (GUILayout.Button("Refresh", GUILayout.Width(70f)))
                FindUsers();
            EditorGUILayout.EndHorizontal();

            if (_users.Count == 0)
                EditorGUILayout.HelpBox("No level is played on this map yet: set it as a level's map.", MessageType.Info);
            foreach (var level in _users)
            {
                using (new EditorGUI.DisabledScope(true))
                    EditorGUILayout.ObjectField(level, typeof(LevelDefinition), false);
            }
            if (_users.Count > 1)
                EditorGUILayout.HelpBox("Every level has its own map; AF/Validate All reports this one.", MessageType.Error);

            using (new EditorGUI.DisabledScope(_users.Count == 0 || (_report != null && _report.ErrorCount > 0) || rows.Length == 0))
            {
                if (GUILayout.Button(_users.Count > 0 ? $"Simulate with {_users[0].name}'s numbers" : "Simulate"))
                    _note = Simulate(_users[0]);
            }
        }

        private string Simulate(LevelDefinition level)
        {
            var map = (MapDefinition)target;
            var config = SimRules.Find<LevelSimConfig>();
            var list = AssetDatabase.LoadAssetAtPath<LevelList>(LevelGenerator.LIST_PATH);
            var index = list != null ? IndexOf(list, level) : -1;
            var grid = MapParser.Parse(map.Rows);
            var rules = SimRules.FromProject();
            var headroom = LevelTuner.Measure(grid, map.StallsPerRestroom, level, rules, config);
            var difficulty = Difficulty.Measure(grid, map.StallsPerRestroom, level, rules, config);
            var reference = difficulty.Reference;
            var goal = index >= 0
                ? string.Format(CultureInfo.InvariantCulture, " (target {0:0.00} for level {1})",
                    config.TargetHeadroom(index, list.Levels.Count), index + 1)
                : string.Empty;
            var needed = !difficulty.IsWinnable ? "cannot be won at any pace tried"
                : string.Format(CultureInfo.InvariantCulture, "needs a path every {0}{1:0.00} s",
                    difficulty.EasierThanRange ? "≥ " : "", difficulty.NeededPace);
            return string.Format(CultureInfo.InvariantCulture,
                "{0}: headroom {1}{2}; {3}. At {4:0.0} s a path: won {5:0%}, {6:0.0} stars, patience {7:0.00} on the way in.",
                level.name, headroom, goal, needed, reference.Pace, reference.WinShare, reference.MeanStars,
                reference.MeanPatienceServed);
        }

        private void DrawGenerate()
        {
            var config = MapGenerateMenu.FindConfig();
            if (config == null || config.Bands.Count == 0)
                return;

            EditorGUILayout.LabelField("Generate", EditorStyles.boldLabel);
            var names = new string[config.Bands.Count];
            for (var i = 0; i < names.Length; i++)
                names[i] = $"{config.Bands[i].Name} ({config.Bands[i].FirstLevel}–{config.Bands[i].LastLevel})";
            _band = Mathf.Clamp(EditorGUILayout.Popup("Recipe", _band, names), 0, names.Length - 1);
            _seed = EditorGUILayout.IntField("Seed", _seed);
            if (!GUILayout.Button("Fill this map from the recipe"))
                return;

            try
            {
                var generated = MapGenerator.Generate(config.Bands[_band].Recipe, MapValidator.ProjectGeometry(),
                    config.Limits, _seed, config.Candidates, config.Attempts);
                WriteRows(generated.Rows);
                _stalls.intValue = generated.Stalls;
                _newRows = generated.Rows.Length;
                _newCols = generated.Rows[0].Length;
                _note = $"Seed {_seed}, try {generated.Attempt}: {generated.Metrics}";
            }
            catch (InvalidOperationException exception)
            {
                _note = exception.Message;
            }
        }

        private string[] ReadRows()
        {
            var rows = new string[_rows.arraySize];
            for (var i = 0; i < rows.Length; i++)
                rows[i] = _rows.GetArrayElementAtIndex(i).stringValue ?? string.Empty;
            return rows;
        }

        // Through the serialized object, so the change is undoable and saved like any Inspector edit.
        private void WriteRows(IReadOnlyList<string> rows)
        {
            _rows.arraySize = rows.Count;
            for (var i = 0; i < rows.Count; i++)
                _rows.GetArrayElementAtIndex(i).stringValue = rows[i];
            _handMade.boolValue = true;
            serializedObject.ApplyModifiedProperties();
            GUI.changed = true;
        }

        private void FindUsers()
        {
            _users.Clear();
            foreach (var guid in AssetDatabase.FindAssets($"t:{nameof(LevelDefinition)}", new[] { "Assets" }))
            {
                var level = AssetDatabase.LoadAssetAtPath<LevelDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                if (level != null && level.Map == target)
                    _users.Add(level);
            }
            _users.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
        }

        private static int IndexOf(LevelList list, LevelDefinition level)
        {
            for (var i = 0; i < list.Levels.Count; i++)
            {
                if (list.Levels[i] == level)
                    return i;
            }
            return -1;
        }
    }
}
