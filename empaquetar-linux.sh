#!/usr/bin/env bash
# Genera un paquete .deb de YoshiSQL para Ubuntu/Debian (64 bits), con icono y entrada en el menú de aplicaciones.
#
# Uso (en Linux, desde la carpeta del proyecto):
#     ./empaquetar-linux.sh
#     sudo apt install ./salida/yoshisql_<version>_amd64.deb
#
# Requisitos: .NET SDK 10 y dpkg-deb (viene con Ubuntu y Debian).
# El programa se publica autocontenido: quien lo instale no necesita tener .NET.
#
# AVISO: este script se escribió desde Windows y todavía no se ha ejecutado en Linux.
# Si falla, el mensaje de error indica en qué paso; conviene revisarlo la primera vez.

set -euo pipefail

RAIZ="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROYECTO="$RAIZ/src/YoshiSQL.Escritorio/YoshiSQL.Escritorio.csproj"
ICONO="$RAIZ/src/YoshiSQL.Escritorio/Recursos/logo.png"
SALIDA="$RAIZ/salida"
PUBLICADO="$SALIDA/publicado"
ARQUITECTURA="amd64"

command -v dotnet >/dev/null || { echo "Falta el SDK de .NET (dotnet)." >&2; exit 1; }
command -v dpkg-deb >/dev/null || { echo "Falta dpkg-deb (sudo apt install dpkg)." >&2; exit 1; }
[ -f "$ICONO" ] || { echo "No se encontró el icono: $ICONO" >&2; exit 1; }

# La versión vive en Directory.Build.props (es común a todos los proyectos), no en el .csproj
VERSION="$(grep -oPm1 '(?<=<Version>)[^<]+' "$RAIZ/Directory.Build.props" || true)"
[ -n "$VERSION" ] || { echo "No se encontró <Version> en Directory.Build.props." >&2; exit 1; }

PAQUETE="$SALIDA/yoshisql_${VERSION}_${ARQUITECTURA}"

echo "==> Publicando YoshiSQL $VERSION para linux-x64..."
rm -rf "$PUBLICADO" "$PAQUETE"
dotnet publish "$PROYECTO" -c Release -r linux-x64 --self-contained true -o "$PUBLICADO"

# El nombre del ejecutable lo decide el proyecto (AssemblyName); se busca en vez de suponerlo
EJECUTABLE="$(find "$PUBLICADO" -maxdepth 1 -type f -executable ! -name '*.so' ! -name '*.dll' -printf '%f\n' | grep -i yoshi | head -n 1 || true)"
[ -n "$EJECUTABLE" ] || { echo "No se encontró el ejecutable publicado en $PUBLICADO" >&2; exit 1; }

echo "==> Armando el paquete..."
mkdir -p "$PAQUETE/DEBIAN" \
         "$PAQUETE/opt/yoshisql" \
         "$PAQUETE/usr/bin" \
         "$PAQUETE/usr/share/applications" \
         "$PAQUETE/usr/share/icons/hicolor/256x256/apps"

cp -r "$PUBLICADO/." "$PAQUETE/opt/yoshisql/"
chmod 755 "$PAQUETE/opt/yoshisql/$EJECUTABLE"
cp "$ICONO" "$PAQUETE/usr/share/icons/hicolor/256x256/apps/yoshisql.png"

# Lanzador en /usr/bin para poder escribir "yoshisql" en una terminal
cat > "$PAQUETE/usr/bin/yoshisql" <<LANZADOR
#!/bin/sh
exec /opt/yoshisql/$EJECUTABLE "\$@"
LANZADOR
chmod 755 "$PAQUETE/usr/bin/yoshisql"

cat > "$PAQUETE/usr/share/applications/yoshisql.desktop" <<ENTRADA
[Desktop Entry]
Type=Application
Name=YoshiSQL
GenericName=Cliente de SQL Server
Comment=Entorno gráfico para SQL Server
Exec=yoshisql %f
Icon=yoshisql
Terminal=false
Categories=Development;Database;
MimeType=application/sql;text/x-sql;
StartupWMClass=YoshiSQL
ENTRADA

TAMANO_KB="$(du -sk "$PAQUETE/opt" | cut -f1)"

cat > "$PAQUETE/DEBIAN/control" <<CONTROL
Package: yoshisql
Version: $VERSION
Section: database
Priority: optional
Architecture: $ARQUITECTURA
Installed-Size: $TAMANO_KB
Depends: libc6, libgcc-s1, libstdc++6, libicu76 | libicu74 | libicu72 | libicu70, libssl3t64 | libssl3, libx11-6, libice6, libsm6, libfontconfig1
Recommends: xdg-utils, libgssapi-krb5-2
Maintainer: JulioCesarGit
Description: Entorno gráfico para SQL Server en Linux
 YoshiSQL es una alternativa ligera a SQL Server Management Studio:
 explorador de objetos, editor de consultas, edición de filas,
 respaldos y exportación de bases completas a un script.
CONTROL

echo "==> Construyendo el .deb..."
dpkg-deb --build --root-owner-group "$PAQUETE" >/dev/null

echo
echo "Listo: ${PAQUETE}.deb"
echo "Para instalarlo:  sudo apt install \"${PAQUETE}.deb\""
