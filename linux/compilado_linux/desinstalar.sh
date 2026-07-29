#!/bin/bash
# desinstalar.sh — quita todo lo que instaló instalar.sh (binario, asociación
# de .enc, entradas de clic derecho). No borra tus archivos .enc, solo la app.
set -e

DIR_INSTALACION="$HOME/.local/share/encriptador"
DIR_APLICACIONES="$HOME/.local/share/applications"
DIR_MIME="$HOME/.local/share/mime/packages"
DIR_SCRIPTS_CAJA="$HOME/.config/caja/scripts"
DIR_SCRIPTS_NAUTILUS="$HOME/.local/share/nautilus/scripts"
DIR_ACCIONES_CAJA="$HOME/.local/share/file-manager/actions"

echo "Desinstalando Encriptador..."

rm -rf "$DIR_INSTALACION"
rm -f "$DIR_APLICACIONES/encriptador.desktop"
rm -f "$DIR_MIME/encriptador-enc.xml"
rm -f "$DIR_SCRIPTS_CAJA/Encriptar con Encriptador"
rm -f "$DIR_SCRIPTS_NAUTILUS/Encriptar con Encriptador"
rm -f "$DIR_ACCIONES_CAJA/encriptador.desktop"

update-mime-database "$HOME/.local/share/mime" >/dev/null 2>&1 || true
update-desktop-database "$DIR_APLICACIONES" >/dev/null 2>&1 || true

echo "Listo. Encriptador se desinstaló por completo."
