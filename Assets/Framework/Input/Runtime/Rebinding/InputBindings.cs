using System;
using System.Collections.Generic;
using AF.Core;
using AF.Save;
using MessagePipe;
using UnityEngine.InputSystem;

namespace AF.Input
{
    /// <summary>
    /// The player's binding overrides, a framework-owned section of the global save data. Fields are only ever added; a
    /// breaking change uses a new key.
    /// </summary>
    public sealed class InputBindingsData
    {
        public List<BindingOverrideData> overrides = new List<BindingOverrideData>();
    }

    public sealed class BindingOverrideData
    {
        /// <summary>Binding id from the actions asset; the override is matched by it alone.</summary>
        public string id;

        /// <summary>"Map/Action", for reading the file only.</summary>
        public string action;

        public string path;
        public string interactions;
        public string processors;
    }

    /// <summary>
    /// Saves the overrides of the actions asset with the global document, so they follow the player to other devices like
    /// gameplay settings. Overrides live in memory only and are lost on a domain reload; this is their only persistence.
    /// </summary>
    internal sealed class InputBindings : Savable<InputBindingsData>
    {
        private readonly InputActionAsset _actions;
        private readonly IPublisher<BindingsChanged> _changed;

        public InputBindings(InputActionAsset actions, IPublisher<BindingsChanged> changed)
        {
            _actions = actions;
            _changed = changed;
        }

        public override string Key => "input.bindings";
        public override SaveDocument Document => SaveDocument.Global;

        protected override InputBindingsData Capture()
        {
            var data = new InputBindingsData();
            foreach (var map in _actions.actionMaps)
            {
                foreach (var binding in map.bindings)
                {
                    if (binding.overridePath == null && binding.overrideInteractions == null && binding.overrideProcessors == null)
                        continue;
                    data.overrides.Add(new BindingOverrideData
                    {
                        id = binding.id.ToString(),
                        action = map.name + "/" + binding.action,
                        path = binding.overridePath,
                        interactions = binding.overrideInteractions,
                        processors = binding.overrideProcessors,
                    });
                }
            }
            return data;
        }

        protected override void Restore(InputBindingsData data)
        {
            _actions.RemoveAllBindingOverrides();
            var bindings = IndexBindings();
            // A file with "overrides": null replaces the field initializer with null.
            foreach (var entry in data.overrides ?? new List<BindingOverrideData>())
            {
                try
                {
                    if (entry == null)
                        continue;
                    if (!Guid.TryParse(entry.id, out var id) || !bindings.TryGetValue(id, out var target))
                    {
                        // The binding was deleted, or deleted and recreated with a new id, since the override was saved.
                        Log.Warning($"Input: the saved override '{entry.path}' of {entry.action} matches no binding of {_actions.name} " +
                                    $"(id {entry.id}) and is dropped. Edit a binding's path in place instead of recreating it.", _actions);
                        continue;
                    }
                    target.action.ApplyBindingOverride(target.index, new InputBinding
                    {
                        overridePath = entry.path,
                        overrideInteractions = entry.interactions,
                        overrideProcessors = entry.processors,
                    });
                }
                catch (Exception exception)
                {
                    // Restore must not throw; one bad entry leaves the others applied.
                    Log.Exception(exception, _actions);
                }
            }
            _changed.Publish(new BindingsChanged(null));
        }

        private Dictionary<Guid, (InputAction action, int index)> IndexBindings()
        {
            var bindings = new Dictionary<Guid, (InputAction, int)>();
            foreach (var map in _actions.actionMaps)
            {
                foreach (var action in map.actions)
                {
                    for (var i = 0; i < action.bindings.Count; i++)
                        bindings[action.bindings[i].id] = (action, i);
                }
            }
            return bindings;
        }
    }
}
