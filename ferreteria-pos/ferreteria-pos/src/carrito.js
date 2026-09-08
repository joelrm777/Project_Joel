const { descuentoMayoreo, iva: ivaConfig } = require('./config');

function calcularTotal(carrito) {
  let subtotal = 0;
  for (const item of carrito) {
    subtotal += item.precio * item.cantidad;
  }

  let descuento = 0;
  if (carrito.some((item) => item.cantidad > descuentoMayoreo.umbralCantidad)) {
    descuento = subtotal * descuentoMayoreo.porcentaje;
  }

  const subtotalConDescuento = subtotal - descuento;
  const iva = subtotalConDescuento * ivaConfig.porcentaje;
  const total = subtotalConDescuento + iva;
  return { subtotal, descuento, iva, total };
}

module.exports = { calcularTotal };
