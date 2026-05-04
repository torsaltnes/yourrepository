# Bootstrap script for Windows (PowerShell)
Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

Write-Host "=== Vaktliste Bootstrap ===" -ForegroundColor Cyan

try { Get-Command node -ErrorAction Stop | Out-Null }
catch { Write-Error "node not found. Please install Node.js 18+."; exit 1 }

try { Get-Command npm -ErrorAction Stop | Out-Null }
catch { Write-Error "npm not found. Please install npm."; exit 1 }

Write-Host "Node version: $(node --version)"
Write-Host "npm  version: $(npm --version)"

Write-Host ""
Write-Host "Installing dependencies..."
npm install

Write-Host ""
Write-Host "=== Done! Next steps ===" -ForegroundColor Green
Write-Host "  npm run dev     - start Vite dev server"
Write-Host "  npm test        - run Vitest test suite"
Write-Host "  npm run build   - production build"
