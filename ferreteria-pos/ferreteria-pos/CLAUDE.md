# CLAUDE.md

Guía para Claude Code (y cualquier otro agente) al trabajar en este repositorio.

## 1. Descripción del proyecto

**Ferretería El Tornillo Feliz — Calculadora de ventas**

Script pequeño en Node.js (sin dependencias externas) que calcula el total de una
venta de caja para la ferretería. Recibe un "carrito" de compra (arreglo de items
con `nombre`, `precio` y `cantidad`) y devuelve:

- `subtotal`: suma de `precio * cantidad` de todos los items.
- `descuento`: 10% sobre el subtotal, aplicado cuando **al menos un item** del
  carrito tiene `cantidad > 10` (compra al por mayor de un solo producto).
- `iva`: 13% (Impuesto al Valor Agregado) calculado sobre el subtotal **ya con
  el descuento aplicado**.
- `total`: subtotal con descuento más el IVA.

### Estructura actual

```
ferreteria-pos/
├── package.json        # metadata + script "test"
├── README.md
├── src/
│   └── carrito.js       # lógica de negocio: calcularTotal(carrito)
└── test/
    └── test-carrito.js  # pruebas con node:assert, corren con `npm test`
```

Punto de entrada de la lógica: `calcularTotal(carrito)` en [src/carrito.js](src/carrito.js).

## 2. Arquitectura a seguir en próximos cambios

Mantener el proyecto simple y sin dependencias innecesarias mientras el alcance
lo permita. Para nuevos cambios:

- **Separación de responsabilidades**: la lógica de cálculo (descuentos, IVA,
  totales) vive en `src/`, separada de cualquier futura capa de presentación
  (CLI, UI de caja, API, etc.). No mezclar entrada/salida (I/O) con reglas de
  negocio dentro de `calcularTotal` u otras funciones de cálculo.
- **Funciones puras**: las funciones de cálculo deben recibir datos y devolver
  datos, sin efectos secundarios (sin `console.log`, sin leer/escribir archivos
  dentro de la lógica de negocio).
- **Un archivo de configuración centralizado**: toda regla de negocio que
  pueda cambiar (porcentaje de descuento, umbral de unidades para mayoreo,
  porcentaje de IVA, moneda, etc.) debe vivir en un archivo de configuración
  dedicado (por ejemplo `src/config.js` o `config.json`), nunca como valor
  "hardcodeado" dentro de la lógica. Las funciones deben importar esos valores
  desde la configuración, no repetirlos como literales.
- **Módulos pequeños y con una sola responsabilidad**: si se agregan nuevas
  reglas de negocio (ej. descuentos por categoría, promociones), crear
  funciones/módulos separados en `src/` en vez de amontonar todo en
  `carrito.js`.
- **Pruebas antes que implementación**: cuando el `README.md` o el usuario
  describan un requerimiento nuevo, revisar primero si ya existe una prueba
  que lo cubra en `test/` antes de escribir código.

## 3. Reglas obligatorias

- **No modificar los archivos de prueba que existen actualmente**
  (`test/test-carrito.js` y cualquier otro archivo de prueba ya presente en el
  repositorio). Las pruebas existentes son la referencia de comportamiento
  esperado; el código de producción (`src/`) es el que se ajusta para
  cumplirlas, nunca al revés. Si se necesita cobertura adicional, se agregan
  pruebas nuevas, no se editan las existentes.
- **Prohibido el "hardcode" de valores variables**. Cualquier valor que
  represente una regla de negocio o parámetro configurable (tasas, umbrales,
  porcentajes, límites, textos de moneda, etc.) debe definirse en un archivo
  de configuración y ser importado desde ahí. No se permiten literales
  numéricos de negocio sueltos dentro de la lógica de cálculo.
