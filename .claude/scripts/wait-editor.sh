#!/usr/bin/env bash
# Waits until the Unity Editor is idle (ready, not compiling, no domain reload), then prints console counts.
# --refresh: run AssetDatabase.Refresh first, so files written while the Editor is unfocused get imported.
# Usage (from the project root): bash .claude/scripts/wait-editor.sh [--refresh] [timeoutSeconds]

refresh=0
if [ "$1" = "--refresh" ]; then refresh=1; shift; fi
timeout=${1:-120}
dir=$(dirname "$0")

if [ $refresh -eq 1 ]; then
  unity command script run --file "$dir/Refresh.cs" --entry ClaudeRefresh.Run --result-only || exit 1
  sleep 3
fi

end=$((SECONDS + timeout))
while [ $SECONDS -lt $end ]; do
  s=$(unity command editor status --result-only 2>/dev/null | tr -d '\n ')
  case "$s" in
    *'"status":"ready"'*'"compiling":false'*'"domainReloadInProgress":false'*)
      unity command console status --result-only 2>&1 | grep -A8 groundTruth
      exit 0 ;;
  esac
  sleep 3
done
echo "timeout after ${timeout}s: $s"
exit 1
