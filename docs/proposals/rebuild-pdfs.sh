#!/usr/bin/env bash
# Regenerate every proposal PDF from its HTML source.
#
# Usage:  bash rebuild-pdfs.sh
#
# IMPORTANT: close any of these PDFs in Acrobat/your viewer first. An open PDF
# is file-locked on Windows and cannot be overwritten; the script will tell you
# which ones are blocked rather than silently skipping them.

set -uo pipefail
cd "$(dirname "$0")"

EDGE="/c/Program Files (x86)/Microsoft/Edge/Application/msedge.exe"
[ -f "$EDGE" ] || EDGE="/c/Program Files/Google/Chrome/Application/chrome.exe"
[ -f "$EDGE" ] || { echo "ERROR: no Edge or Chrome found for PDF rendering"; exit 1; }

# A dedicated profile dir avoids the "Missing headless user data directory" error.
UDD="$(mktemp -d 2>/dev/null || echo "/tmp/edge-pdf-$$")"
trap 'rm -rf "$UDD"' EXIT

# source-html basename  ->  output PDF basename
build() {
  local src="$1" out="$2"
  if [ -f "$out.pdf" ] && ! (exec 3>>"$out.pdf") 2>/dev/null; then
    echo "  LOCKED  $out.pdf  — close it in your PDF viewer, then re-run"
    return 1
  fi
  rm -f "$out.pdf"
  "$EDGE" --headless=new --disable-gpu --user-data-dir="$UDD" \
          --no-pdf-header-footer --print-to-pdf="$out.pdf" \
          "file:///$(pwd)/source-html/$src.html" 2>/dev/null
  sleep 3
  if [ -f "$out.pdf" ]; then
    echo "  OK      $out.pdf  ($(stat -c%s "$out.pdf") bytes)"
  else
    echo "  FAILED  $out.pdf"
    return 1
  fi
}

echo "Rebuilding proposal PDFs..."
fail=0
build "00-Complete-Client-Proposal"       "00-Ophir-Website-Complete-Proposal" || fail=1
build "01-Website-Plan-and-Architecture"  "01-Website-Plan-and-Architecture"   || fail=1
build "02-Homepage-Design-Draft"          "02-Homepage-Design-Draft"           || fail=1
build "03-Website-Cost-Proposal"          "03-Website-Cost-Proposal"           || fail=1
build "04-Client-Proposal"                "04-Client-Proposal"                 || fail=1
build "05-Architecture-Decisions"         "05-Architecture-Decisions"          || fail=1

echo
if [ "$fail" -eq 0 ]; then
  echo "All PDFs rebuilt successfully."
else
  echo "Some PDFs could not be rebuilt (see above). Close them in your viewer and re-run."
  exit 1
fi
