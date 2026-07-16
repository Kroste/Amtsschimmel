#!/usr/bin/env bash
# Liest die Version aus Directory.Build.props, prueft den Git-Zustand,
# erstellt einen annotierten Tag vX.Y.Z und pusht ihn (loest die Release-Action aus).
set -euo pipefail

VERSION=$(grep -oP '(?<=<Version>)[^<]+' Directory.Build.props)
TAG="v${VERSION}"

if [ -n "$(git status --porcelain)" ]; then
  echo "FEHLER: Es gibt uncommittete Aenderungen. Erst committen." >&2
  exit 1
fi
if [ -n "$(git log --branches --not --remotes --oneline)" ]; then
  echo "FEHLER: Es gibt ungepushte Commits. Erst pushen." >&2
  exit 1
fi
if git rev-parse "$TAG" >/dev/null 2>&1; then
  read -r -p "Tag $TAG existiert bereits. Loeschen und neu setzen? [j/N] " answer
  if [ "${answer,,}" != "j" ]; then
    echo "Abgebrochen."
    exit 0
  fi
  git tag -d "$TAG"
  git push origin ":refs/tags/$TAG" || true
fi

git tag -a "$TAG" -m "Release $TAG"
git push origin "$TAG"
echo "Tag $TAG gepusht - die Release-Action baut jetzt die Pakete."
