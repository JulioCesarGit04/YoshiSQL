-- ============================================================================
-- Base de prueba para practicar con los planes de ejecución en YoshiSQL
-- Tablas: Clientes (2.000 filas), Productos (100 filas), Pedidos (100.000 filas)
-- Ejecuta todo el script con F5. Tarda unos segundos por la cantidad de filas.
-- ============================================================================

USE master;
GO

-- Si ya existe de una prueba anterior, se borra para empezar de cero
IF DB_ID(N'PruebaPlanes') IS NOT NULL
BEGIN
    ALTER DATABASE PruebaPlanes SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE PruebaPlanes;
END
GO

CREATE DATABASE PruebaPlanes;
GO

USE PruebaPlanes;
GO

-- ----------------------------------------------------------------------------
-- Tablas
-- ----------------------------------------------------------------------------
CREATE TABLE dbo.Clientes
(
    Id INT IDENTITY(1,1) NOT NULL,
    Nombre NVARCHAR(100) NOT NULL,
    Ciudad NVARCHAR(50) NOT NULL,
    FechaDeRegistro DATE NOT NULL,
    CONSTRAINT PK_Clientes PRIMARY KEY (Id)
);

CREATE TABLE dbo.Productos
(
    Id INT IDENTITY(1,1) NOT NULL,
    Nombre NVARCHAR(100) NOT NULL,
    Categoria NVARCHAR(50) NOT NULL,
    Precio DECIMAL(10,2) NOT NULL,
    CONSTRAINT PK_Productos PRIMARY KEY (Id)
);

-- A propósito NO tiene índice en ClienteId: así el plan mostrará el problema
CREATE TABLE dbo.Pedidos
(
    Id INT IDENTITY(1,1) NOT NULL,
    ClienteId INT NOT NULL,
    ProductoId INT NOT NULL,
    Cantidad INT NOT NULL,
    Fecha DATE NOT NULL,
    Total DECIMAL(12,2) NOT NULL,
    CONSTRAINT PK_Pedidos PRIMARY KEY (Id),
    CONSTRAINT FK_Pedidos_Clientes FOREIGN KEY (ClienteId) REFERENCES dbo.Clientes (Id),
    CONSTRAINT FK_Pedidos_Productos FOREIGN KEY (ProductoId) REFERENCES dbo.Productos (Id)
);
GO

-- ----------------------------------------------------------------------------
-- Datos: se generan con una lista de números en lugar de miles de INSERT
-- ----------------------------------------------------------------------------
SET NOCOUNT ON;

-- 2.000 clientes repartidos en 8 ciudades
WITH Numeros AS
(
    SELECT TOP (2000) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS Numero
    FROM sys.all_objects AS a CROSS JOIN sys.all_objects AS b
)
INSERT INTO dbo.Clientes (Nombre, Ciudad, FechaDeRegistro)
SELECT
    CONCAT(N'Cliente ', Numero),
    CHOOSE((Numero % 8) + 1, N'Lima', N'Arequipa', N'Cusco', N'Trujillo', N'Piura', N'Chiclayo', N'Iquitos', N'Tacna'),
    DATEADD(DAY, -(Numero % 1500), CAST('2026-10-01' AS DATE))
FROM Numeros;

-- 100 productos en 5 categorías
WITH Numeros AS
(
    SELECT TOP (100) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS Numero
    FROM sys.all_objects
)
INSERT INTO dbo.Productos (Nombre, Categoria, Precio)
SELECT
    CONCAT(N'Producto ', Numero),
    CHOOSE((Numero % 5) + 1, N'Bebidas', N'Lácteos', N'Limpieza', N'Abarrotes', N'Snacks'),
    CAST(5 + (Numero * 7 % 200) AS DECIMAL(10,2))
FROM Numeros;

-- 100.000 pedidos de los últimos 2 años
WITH Numeros AS
(
    SELECT TOP (100000) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS Numero
    FROM sys.all_objects AS a CROSS JOIN sys.all_objects AS b
)
INSERT INTO dbo.Pedidos (ClienteId, ProductoId, Cantidad, Fecha, Total)
SELECT
    (Numero * 7919 % 2000) + 1,
    (Numero * 104729 % 100) + 1,
    (Numero % 10) + 1,
    DATEADD(DAY, -(Numero % 730), CAST('2026-10-01' AS DATE)),
    CAST(((Numero % 10) + 1) * (5 + (Numero % 200)) AS DECIMAL(12,2))
FROM Numeros;

SET NOCOUNT OFF;
GO

-- Comprobación: cuántas filas quedaron en cada tabla
SELECT N'Clientes' AS Tabla, COUNT(*) AS Filas FROM dbo.Clientes
UNION ALL SELECT N'Productos', COUNT(*) FROM dbo.Productos
UNION ALL SELECT N'Pedidos', COUNT(*) FROM dbo.Pedidos;
GO


-- ============================================================================
-- PRUEBAS: no ejecutes esto con F5 todo junto. Subraya cada consulta y usa
-- Ctrl+L (plan estimado) o activa "Plan real" (Ctrl+M) y luego F5.
-- ============================================================================

-- Prueba 1: pedidos de un cliente (sin índice en ClienteId)
-- Esperado: "Clustered Index Scan" sobre Pedidos (recorre las 100.000 filas)
-- y la advertencia "Falta un índice que mejoraría esta consulta".
SELECT p.Id, p.Fecha, p.Total
FROM dbo.Pedidos AS p
WHERE p.ClienteId = 25;

-- Prueba 2: ventas por ciudad (une las tres tablas)
-- Esperado: varias operaciones; mira cuál tiene el porcentaje más alto.
SELECT c.Ciudad, pr.Categoria, SUM(p.Total) AS TotalVendido, COUNT(*) AS Pedidos
FROM dbo.Pedidos AS p
INNER JOIN dbo.Clientes AS c ON c.Id = p.ClienteId
INNER JOIN dbo.Productos AS pr ON pr.Id = p.ProductoId
WHERE c.Ciudad = N'Lima'
GROUP BY c.Ciudad, pr.Categoria
ORDER BY TotalVendido DESC;

-- Prueba 3: la solución. Crea el índice que faltaba...
CREATE NONCLUSTERED INDEX IX_Pedidos_ClienteId ON dbo.Pedidos (ClienteId) INCLUDE (Fecha, Total);

-- ...y vuelve a ver el plan de la Prueba 1.
-- Esperado: ahora dice "Index Seek" (va directo a las filas del cliente)
-- y la advertencia de índice faltante desaparece.

-- Prueba 4: búsqueda que no puede usar índice por la función sobre la columna
-- Esperado: "Scan" aunque haya índice. Compárala con la Prueba 5.
SELECT Id, Total FROM dbo.Pedidos WHERE YEAR(Fecha) = 2026;

-- Prueba 5: la misma búsqueda escrita sin función sobre la columna
CREATE NONCLUSTERED INDEX IX_Pedidos_Fecha ON dbo.Pedidos (Fecha) INCLUDE (Total);
SELECT Id, Total FROM dbo.Pedidos WHERE Fecha >= '2026-01-01' AND Fecha < '2027-01-01';
