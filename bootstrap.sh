#!/usr/bin/env bash
set -euo pipefail

echo "=== Vaktliste Bootstrap ==="

if ! command -v node &>/dev/null; then
  echo "ERROR: node not found. Please install Node.js 18+." >&2
  exit 1
fi

if ! command -v npm &>/dev/null; then
  echo "ERROR: npm not found. Please install npm." >&2
  exit 1
fi

echo "Node version: $(node --version)"
echo "npm  version: $(npm --version)"

echo ""
echo "Installing dependencies..."
npm install

echo ""
echo "=== Done! Next steps ==="
echo "  npm run dev     – start Vite dev server"
echo "  npm test        – run Vitest test suite"
echo "  npm run build   – production build"
