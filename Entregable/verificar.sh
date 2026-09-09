#!/usr/bin/env bash
# Puerta de calidad. 0 = se puede cerrar. 2 = algo falló.
set -u

dotnet test backend/tests/MileageClaims.Tests --nologo || { echo "La suite de pruebas del backend falló." >&2; exit 2; }

(cd frontend && npm run build) || { echo "El chequeo de tipos / build del frontend falló." >&2; exit 2; }

echo "Verificación completa."
