// Vuelve a consultar el mapa cada 5 segundos y lo redibuja sin recargar la página.
// La ventana de 5 segundos deja un caso en el que otro comprador toma la butaca antes; el
// rechazo del núcleo lo cubre (DISENO.md, componente 2 y decisión 5).
(function () {
    const mapa = document.getElementById('mapa');
    if (!mapa) {
        return;
    }

    const funcionId = mapa.dataset.funcionId;
    const aviso = document.getElementById('aviso');
    const clasesDeEstado = ['libre', 'apartada', 'vendida', 'novendible'];

    async function refrescar() {
        try {
            const respuesta = await fetch(`/api/funciones/${funcionId}/mapa`, { cache: 'no-store' });
            if (!respuesta.ok) {
                return;
            }

            const datos = await respuesta.json();
            for (const butaca of datos.butacas) {
                const elemento = mapa.querySelector(`[data-butaca="${butaca.fila}${butaca.numero}"]`);
                if (!elemento) {
                    continue;
                }

                const estado = String(butaca.estado).toLowerCase();
                elemento.classList.remove(...clasesDeEstado);
                elemento.classList.add(estado);
                elemento.title = `${butaca.fila}${butaca.numero} — ${butaca.estado}`;
            }

            aviso.textContent = 'Mapa actualizado a las ' + new Date().toLocaleTimeString('es-CR');
        } catch {
            aviso.textContent = 'No se pudo actualizar el mapa. Se vuelve a intentar en 5 segundos.';
        }
    }

    setInterval(refrescar, 5000);
})();
