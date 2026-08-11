# Encargo inicial

Haciendo uso de la Skill "escribir-diseno" que se encuentra en esta misma carpeta, necesito que creemos un archivo de Especificaciones.md y de Diseno.md

En el de Especificaciones debemos incluir, lo que debe hacer el sistema y que reglas debe seguir, no ocupo en este tecnologias a utilizar, es un archivo mas funcional, en el segundo archivo el de Diseno aca si debemos colocar la arquitectura de la aplicacion, como se componen, los limites entre ellas y que se promete entre ellas, el modelo de datos a utilizar y las alternativas consideras y la razon por la cual se eligio cada una.
Y necesito un tercer archivo que se llame Prompt.md, donde se va incluir este primer comentario que te estoy indicando.

Todo esto lo vamos a realizar con el analisis de la siguiente necesidad que tenemos

Contexto: El Cine Variedades es una sala independiente con dos auditorios: uno de 120 butacas y otro de 60. Programa entre tres y cuatro funciones diarias en cada uno y cambia la cartelera los jueves.
La venta ocurre hoy únicamente en taquilla. Quien atiende lleva un cuaderno y un mapa de butacas impreso para cada función, que va marcando con lápiz. La dueña quiere vender por internet para descongestionar la fila de viernes y sábado, que es cuando la sala se llena.
De la primera conversación con ella quedaron anotadas estas frases:
«Los miércoles la entrada vale la mitad.»
«A los estudiantes les hacemos precio, pero tienen que mostrar el carné.»
«Cuando el proyector falla hay que devolver la plata. Pasa dos o tres veces al año.»
«El distribuidor me pide todos los meses cuántos boletos se vendieron de cada película.»
«Lo que yo quiero es que la gente escoja su asiento desde el teléfono.»
No existe más documentación. Todo lo que el sistema deba hacer, más allá de lo anterior, hay que averiguarlo.
Alcance
El sistema se limita a lo siguiente:
- Un solo cine, con sus dos salas.
- La cartelera de una semana.
- El pago se simula: el sistema registra que la compra quedó pagada, sin conectarse a ningún medio de pago real.
- Sin emisión de boletos impresos ni códigos de barras.
- Sin cuentas de usuario, salvo las que la propia especificación determine que hacen falta.
Cualquier otra restricción de alcance que se adopte debe quedar escrita como decisión y no darse por supuesta.
