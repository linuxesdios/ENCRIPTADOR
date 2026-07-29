#!/bin/bash
# instalar.sh — instala Encriptador para el usuario actual (sin necesitar sudo):
#   - copia el binario a ~/.local/share/encriptador/
#   - asocia los archivos .enc para que se abran con doble clic / "Abrir con"
#   - agrega "Encriptar con Encriptador" al clic derecho:
#       * carpeta "Scripts" de Caja/Nautilus (funciona siempre, sin instalar nada extra)
#       * item directo de Caja Actions (si el paquete "caja-actions" está instalado)
set -e

DIR_ORIGEN="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
DIR_INSTALACION="$HOME/.local/share/encriptador"
DIR_APLICACIONES="$HOME/.local/share/applications"
DIR_MIME="$HOME/.local/share/mime/packages"
DIR_SCRIPTS_CAJA="$HOME/.config/caja/scripts"
DIR_SCRIPTS_NAUTILUS="$HOME/.local/share/nautilus/scripts"
DIR_ACCIONES_CAJA="$HOME/.local/share/file-manager/actions"

if [ ! -f "$DIR_ORIGEN/Encriptador" ]; then
    echo "No se encontró '$DIR_ORIGEN/Encriptador'. Ejecutá este script desde la carpeta compilado_linux."
    exit 1
fi

echo "Instalando Encriptador en $DIR_INSTALACION ..."
mkdir -p "$DIR_INSTALACION"
cp "$DIR_ORIGEN/Encriptador" "$DIR_INSTALACION/Encriptador"
chmod +x "$DIR_INSTALACION/Encriptador"
if [ -f "$DIR_ORIGEN/icono.png" ]; then
    cp "$DIR_ORIGEN/icono.png" "$DIR_INSTALACION/icono.png"
fi

# --- Asociación de archivos .enc (doble clic / "Abrir con") ---
mkdir -p "$DIR_MIME"
cat > "$DIR_MIME/encriptador-enc.xml" <<EOF
<?xml version="1.0" encoding="UTF-8"?>
<mime-info xmlns="http://www.freedesktop.org/standards/shared-mime-info">
  <mime-type type="application/x-encriptador-enc">
    <comment>Archivo encriptado (Encriptador)</comment>
    <glob pattern="*.enc"/>
  </mime-type>
</mime-info>
EOF

mkdir -p "$DIR_APLICACIONES"
cat > "$DIR_APLICACIONES/encriptador.desktop" <<EOF
[Desktop Entry]
Type=Application
Name=Encriptador
Comment=Cifrado AES-256-GCM de archivos
Exec=$DIR_INSTALACION/Encriptador %f
Icon=$DIR_INSTALACION/icono.png
Terminal=false
Categories=Utility;Security;
MimeType=application/x-encriptador-enc;
EOF

update-mime-database "$HOME/.local/share/mime" >/dev/null 2>&1 || true
update-desktop-database "$DIR_APLICACIONES" >/dev/null 2>&1 || true
xdg-mime default encriptador.desktop application/x-encriptador-enc >/dev/null 2>&1 || true

# --- Clic derecho, opción 1: carpeta "Scripts" (Caja y Nautilus) ---
for DIR_SCRIPTS in "$DIR_SCRIPTS_CAJA" "$DIR_SCRIPTS_NAUTILUS"; do
    mkdir -p "$DIR_SCRIPTS"
    cat > "$DIR_SCRIPTS/Encriptar con Encriptador" <<EOF
#!/bin/bash
IFS=\$'\n'
RUTAS="\${CAJA_SCRIPT_SELECTED_FILE_PATHS:-\$NAUTILUS_SCRIPT_SELECTED_FILE_PATHS}"
for archivo in \$RUTAS; do
    "$DIR_INSTALACION/Encriptador" "\$archivo" &
done
EOF
    chmod +x "$DIR_SCRIPTS/Encriptar con Encriptador"
done

# --- Clic derecho, opción 2: item directo vía Caja Actions (si está instalado) ---
# Formato "best effort": Caja Actions cambió de esquema entre versiones de MATE,
# así que esto puede no aparecer en todas las distros. Si no aparece, usá la
# opción 1 (clic derecho -> Scripts -> "Encriptar con Encriptador"), que siempre funciona.
mkdir -p "$DIR_ACCIONES_CAJA"
cat > "$DIR_ACCIONES_CAJA/encriptador.desktop" <<EOF
[Desktop Entry]
Type=Action
Name=Encriptar con Encriptador
Icon=$DIR_INSTALACION/icono.png
Profiles=profile-zero;

[X-Action-Profile profile-zero]
Name=Default profile
Exec=$DIR_INSTALACION/Encriptador %f
MimeTypes=*/*;
SelectionCount=>0
EOF

echo ""
echo "Listo."
echo " - Doble clic / 'Abrir con' en un .enc ya debería ofrecer Encriptador."
echo " - Clic derecho -> Scripts -> 'Encriptar con Encriptador' (funciona seguro en Caja/Nautilus)."
echo " - Si tenés 'caja-actions' instalado, también debería aparecer como ítem directo"
echo "   'Encriptar con Encriptador' en el clic derecho (puede necesitar reiniciar Caja: caja -q)."
echo " - Para desinstalar: ./desinstalar.sh"
