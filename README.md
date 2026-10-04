# YoshiSQL

Entorno gráfico de escritorio, libre y ligero, para administrar **SQL Server desde Linux**.

SQL Server se puede instalar en Linux, pero SQL Server Management Studio (SSMS) solo existe para Windows. YoshiSQL ofrece lo esencial de SSMS con una interfaz moderna: explorar bases de datos, escribir y ejecutar consultas, diseñar tablas, ver diagramas y planes de ejecución, y administrar el servidor.

Hecho con **C#, .NET 10 y Avalonia UI**. Todas sus dependencias son de código abierto.

---

## Funciones

### Conexión y explorador de objetos
- Conexiones guardadas, con la contraseña cifrada en tu equipo.
- Árbol desplegable como en SSMS: bases de datos, tablas, vistas, procedimientos, funciones, columnas, índices y seguridad (inicios de sesión, usuarios y roles).
- Menú contextual con las acciones de cada objeto: seleccionar filas, editar, diseñar, modificar, generar scripts `CREATE` y `DROP`, respaldar y restaurar.

### Editor de consultas
- Resaltado de sintaxis T-SQL y **autocompletado** de tablas, columnas, palabras clave y funciones.
- Ejecuta todo el script o **solo el texto subrayado**, respetando los separadores `GO` y `GO n`.
- Varias pestañas, abrir y guardar archivos `.sql`, buscar y reemplazar, y formatear el código.
- Doble clic en un error para saltar a su línea.

### Resultados
- Grilla con número de fila y `NULL` visibles, y pestaña de mensajes.
- Exportar a **CSV, Excel o JSON**, o copiar con encabezados para pegar en una hoja de cálculo.
- Historial de consultas con buscador.

### Diseño y datos
- **Diseñador de tablas**: crea tablas y modifica columnas. Los cambios se aplican en una transacción y puedes ver el script antes.
- **Editar las primeras 200 filas** directamente en la grilla, con comandos parametrizados en una transacción.
- **Diagramas de base de datos** con tablas que se pueden mover y relaciones dibujadas automáticamente.

### Administración
- **Monitor de actividad**: sesiones conectadas, consultas en ejecución, bloqueos y opción de terminar un proceso.
- **Respaldo y restauración** de bases de datos con asistentes.
- **Plan de ejecución** estimado y real, mostrado como un árbol con el costo de cada paso y advertencias como índices faltantes.

### Robustez y preferencias
- Si algo falla, YoshiSQL muestra un aviso claro con un **código de error** y guarda el detalle en un registro legible.
- Autoguardado de las pestañas y recuperación de scripts si el programa se cierra de forma inesperada.
- Tema oscuro, claro o según el sistema, y tipo y tamaño de letra del editor configurables.

---

## Requisitos

| Componente | Versión |
|---|---|
| Sistema operativo | Linux (probado en Ubuntu 24.04) |
| .NET SDK | 10 |
| SQL Server | 2019 o superior (probado con SQL Server 2025) |

Instalar .NET 10 en Ubuntu:

```bash
sudo apt install -y dotnet-sdk-10.0
```

---

## Instalación y uso

```bash
git clone https://github.com/TU_USUARIO/YoshiSQL.git
cd YoshiSQL
dotnet run --project src/YoshiSQL.Escritorio
```

Al abrir, pulsa **Conectar al servidor** e ingresa los datos de tu SQL Server. En una instalación local:

- **Servidor:** `localhost`
- **Usuario:** `sa` y la contraseña que definiste al instalar SQL Server
- Deja marcado **"Confiar en el certificado del servidor"**: las instalaciones locales usan un certificado autofirmado.

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

---

## Dónde guarda sus archivos

| Carpeta | Contenido |
|---|---|
| `~/.config/yoshisql/` | Conexiones guardadas, contraseñas cifradas, preferencias y posiciones de los diagramas |
| `~/.local/state/yoshisql/` | Registros de errores (se conservan 14 días), pestañas abiertas, historial y scripts recuperados |

Las carpetas y archivos se crean con permisos que solo tu usuario puede leer.

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
- Compatibilidad completa con Windows y macOS.
- Soporte para otros motores, como PostgreSQL o MySQL.

---

## Tecnologías

- [.NET 10](https://dotnet.microsoft.com/) y C#
- [Avalonia UI](https://avaloniaui.net/) y [AvaloniaEdit](https://github.com/AvaloniaUI/AvaloniaEdit) para la interfaz y el editor
- [Microsoft.Data.SqlClient](https://github.com/dotnet/SqlClient) para conectarse a SQL Server
- [ScriptDom](https://github.com/microsoft/SqlScriptDOM), el analizador oficial de T-SQL, para dividir lotes, formatear y autocompletar
- [CommunityToolkit.Mvvm](https://github.com/CommunityToolkit/dotnet) y [Serilog](https://serilog.net/)
