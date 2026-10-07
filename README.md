# YoshiSQL

Entorno gráfico de escritorio, libre y ligero, para administrar **SQL Server desde Linux y Windows**.

SQL Server se puede instalar en Linux, pero SQL Server Management Studio (SSMS) solo existe para Windows. YoshiSQL ofrece lo esencial de SSMS con una interfaz moderna, y la misma aplicación funciona en ambos sistemas: explorar bases de datos, escribir y ejecutar consultas, diseñar tablas, ver diagramas y planes de ejecución, y administrar el servidor.

Hecho con **C#, .NET 10 y Avalonia UI**. Todas sus dependencias son de código abierto.

---

## Funciones

### Conexión y explorador de objetos
- Conexiones guardadas, con la contraseña cifrada en tu equipo y un color opcional para distinguirlas en la barra de estado (por ejemplo, rojo para producción).
- Árbol desplegable como en SSMS: bases de datos, tablas, vistas, procedimientos, funciones, columnas, índices, disparadores y seguridad (inicios de sesión, usuarios y roles), con un filtro por nombre.
- Menú contextual con las acciones de cada objeto: seleccionar filas, editar, diseñar, modificar, generar scripts `CREATE`, `DROP`, `SELECT`, `INSERT`, `UPDATE` y `DELETE`, ver propiedades (de tablas y bases de datos), ver dependencias, respaldar y restaurar.

### Editor de consultas
- Resaltado de sintaxis T-SQL y **autocompletado** de tablas, columnas, palabras clave y funciones.
- Ejecuta todo el script o **solo el texto subrayado**, respetando los separadores `GO` y `GO n`.
- Varias pestañas, abrir y guardar archivos `.sql`, buscar y reemplazar, formatear, comentar o descomentar (`Ctrl+/`) y cambiar mayúsculas/minúsculas.
- Doble clic en un error para saltar a su línea.

### Resultados
- Grilla con número de fila y `NULL` visibles, pestaña de mensajes, resultados en texto plano con `Ctrl+T`, y un visor para ver el valor de una celda con el XML o JSON formateado.
- Exportar a **CSV, Excel o JSON**, copiar con encabezados para pegar en una hoja de cálculo, o copiar las filas como sentencias `INSERT`.
- Historial de consultas con buscador.

### Diseño y datos
- **Diseñador de tablas**: crea tablas y modifica columnas. Los cambios se aplican en una transacción y puedes ver el script antes.
- **Editar las primeras N filas** directamente en la grilla, con comandos parametrizados en una transacción. Puedes filtrar con `WHERE` y `ORDER BY`, un filtro rápido sobre las filas cargadas, poner una celda en `NULL` con `Ctrl+0`, y guardar cada fila al salir de ella o todas juntas con un botón.
- **Diagramas de base de datos** con tablas que se pueden mover y relaciones dibujadas automáticamente.
- **Exportar una base a un `.sql`** eligiendo qué incluir (estructura, datos, vistas, procedimientos y funciones), para recrearla en cualquier SQL Server o versionarla, sin depender de un `.bak`.

### Administración
- **Monitor de actividad**: sesiones conectadas, consultas en ejecución, bloqueos y opción de terminar un proceso.
- **Respaldo y restauración** de bases de datos con asistentes.
- **Plan de ejecución** estimado y real, mostrado como un árbol con el costo de cada paso y advertencias como índices faltantes.
- **Estadísticas de IO y tiempo** (`SET STATISTICS`), **visor del log de errores del servidor** y **fragmentación de índices** con reconstruir/reorganizar.

### Robustez y preferencias
- Si algo falla, YoshiSQL muestra un aviso claro con un **código de error** y guarda el detalle en un registro legible.
- Autoguardado de las pestañas y recuperación de scripts si el programa se cierra de forma inesperada.
- Tema oscuro, claro o según el sistema, tipo y tamaño de letra del editor, y la cantidad de filas al seleccionar y al editar, todo configurable.

---

## Requisitos

| Componente | Versión |
|---|---|
| Sistema operativo | Linux (probado en Ubuntu 24.04) o Windows 10/11 de 64 bits |
| .NET SDK | 10 (solo para compilar o ejecutar desde el código) |
| SQL Server | 2019 o superior (probado con SQL Server 2025) |

Instalar .NET 10:

```bash
# Ubuntu
sudo apt install -y dotnet-sdk-10.0
```

```powershell
# Windows (PowerShell)
winget install Microsoft.DotNet.SDK.10
```

---

## Instalación y uso

Los mismos comandos funcionan en la terminal de Linux y en PowerShell de Windows:

```bash
git clone https://github.com/JulioCesarGit04/YoshiSQL.git
cd YoshiSQL
dotnet run --project src/YoshiSQL.Escritorio
```

### Crear un ejecutable

Genera una versión que no necesita .NET instalado en el equipo donde se use:

```bash
# Para Windows: crea publicar/windows/YoshiSQL.exe
dotnet publish src/YoshiSQL.Escritorio -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publicar/windows

# Para Linux: crea publicar/linux/YoshiSQL
dotnet publish src/YoshiSQL.Escritorio -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publicar/linux
```

Se puede compilar para Windows desde Linux y al revés.

Al abrir, pulsa **Conectar al servidor** e ingresa los datos de tu SQL Server. En una instalación local:

- **Servidor:** `localhost`
- **Usuario:** `sa` y la contraseña que definiste al instalar SQL Server
- Deja marcado **"Confiar en el certificado del servidor"**: las instalaciones locales usan un certificado autofirmado.

En Windows también puedes elegir **Autenticación de Windows** para entrar con tu cuenta de usuario, sin contraseña. Para una instancia con nombre (por ejemplo SQL Server Express), escribe el servidor como `localhost\SQLEXPRESS`.

En `ejemplos/PruebaDePlanes.sql` hay una base de datos de prueba con 100.000 filas para practicar con los planes de ejecución.

---

## Atajos de teclado

| Atajo | Acción | Atajo | Acción |
|---|---|---|---|
| `F5` / `Ctrl+E` | Ejecutar | `Ctrl+N` | Nueva consulta |
| `Alt+Pause` | Cancelar ejecución | `Ctrl+O` | Abrir archivo |
| `Ctrl+L` | Plan estimado | `Ctrl+S` | Guardar |
| `Ctrl+M` | Incluir plan real | `Ctrl+Shift+S` | Guardar como |
| `Ctrl+Espacio` | Autocompletar | `Ctrl+W` | Cerrar pestaña |
| `Ctrl+F` | Buscar | `Ctrl+H` | Reemplazar |
| `Ctrl+Shift+F` | Formatear SQL | `Ctrl+,` | Preferencias |
| `Ctrl+/` | Comentar/descomentar | `Ctrl+Shift+U` / `Ctrl+Shift+L` | MAYÚSCULAS / minúsculas |

---

## Dónde guarda sus archivos

| Linux | Windows | Contenido |
|---|---|---|
| `~/.config/yoshisql/` | `%APPDATA%\YoshiSQL\` | Conexiones guardadas, contraseñas cifradas, preferencias y posiciones de los diagramas |
| `~/.local/state/yoshisql/` | `%LOCALAPPDATA%\YoshiSQL\` | Registros de errores (se conservan 14 días), pestañas abiertas, historial y scripts recuperados |

Las contraseñas se guardan cifradas. En Linux, las carpetas se crean con permisos que solo tu usuario puede leer; en Windows, la clave de cifrado se protege además con DPAPI, de modo que solo tu cuenta de Windows puede usarla.

---

## Estructura del proyecto

El código sigue una arquitectura en capas; las dependencias siempre apuntan hacia el Dominio:

```
src/
├── YoshiSQL.Dominio            Entidades y contratos; no depende de nada externo
├── YoshiSQL.Aplicacion         Casos de uso: conectar, ejecutar, exportar, diseñar...
├── YoshiSQL.Infraestructura    SQL Server, archivos, cifrado y exportadores
└── YoshiSQL.Escritorio         Interfaz gráfica con Avalonia (patrón MVVM)
pruebas/                        Pruebas automáticas de cada capa
```

Para soportar otro motor de base de datos basta con agregar un nuevo proveedor en Infraestructura, sin tocar las demás capas. El detalle de la arquitectura y las convenciones de código está en `ESTRUCTURA_YOSHISQL.txt`.

Ejecutar las pruebas:

```bash
dotnet test
```

---

## Estado del proyecto

Versión **0.5.0**, en desarrollo activo.

Pendiente:
- Instalador para Linux (`.deb` o AppImage) con ícono en el menú de aplicaciones.
- Valores predeterminados (`DEFAULT`) en el diseñador de tablas.
- Probar a fondo la versión de Windows 11 y agregar soporte para macOS.
- Soporte para otros motores, como PostgreSQL o MySQL.

---

## Tecnologías

- [.NET 10](https://dotnet.microsoft.com/) y C#
- [Avalonia UI](https://avaloniaui.net/) y [AvaloniaEdit](https://github.com/AvaloniaUI/AvaloniaEdit) para la interfaz y el editor
- [Microsoft.Data.SqlClient](https://github.com/dotnet/SqlClient) para conectarse a SQL Server
- [ScriptDom](https://github.com/microsoft/SqlScriptDOM), el analizador oficial de T-SQL, para dividir lotes, formatear y autocompletar
- [CommunityToolkit.Mvvm](https://github.com/CommunityToolkit/dotnet) y [Serilog](https://serilog.net/)
