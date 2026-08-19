// El mapa de la función: elegir butacas, soltarlas y pagar.
// Se vuelve a consultar cada 5 segundos y se redibuja sin recargar la página. La ventana de 5
// segundos deja el caso en que otro comprador toma la butaca antes; el rechazo del núcleo lo
// cubre y devuelve el mapa fresco (DISENO.md, componente 2 y decisión 5).
(function () {
    const mapa = document.getElementById('mapa');
    if (!mapa) {
        return;
    }

    const funcionId = mapa.dataset.funcionId;
    const panel = document.getElementById('compra');
    const tarifa = Number(panel.dataset.tarifa);
    const aviso = document.getElementById('aviso');
    const seleccionTexto = document.getElementById('seleccion');
    const plazo = document.getElementById('plazo');
    const error = document.getElementById('error');
    const botonPagar = document.getElementById('pagar');
    const correo = document.getElementById('correo');
    const confirmacion = document.getElementById('confirmacion');
    const clasesDeEstado = ['libre', 'apartada', 'vendida', 'novendible'];

    // Lo que este comprador tiene apartado en este intento.
    let apartadoId = null;
    let venceEn = null;
    let mias = new Set();
    let pagando = false;
    let claveIdempotencia = null;

    const motivos = {
        ButacaTomada: 'Esa butaca ya no está disponible.',
        ApartadoVencido: 'Pasaron los 10 minutos y sus butacas volvieron a estar libres. Empiece de nuevo.',
        FuncionCerrada: 'La venta en línea de esta función ya cerró.',
        FuncionCancelada: 'Esta función fue cancelada.',
        ButacaNoVendible: 'Esa butaca no se vende.',
        LimiteButacas: 'Una compra admite hasta 10 butacas.',
        TarifaNoDisponible: 'Esa tarifa no está disponible para esta función.',
        EdadNoDeclarada: 'Falta declarar que cumple la edad mínima.',
        ButacaNoApartada: 'Esa butaca no está en su selección.',
        ApartadoNoEncontrado: 'Su selección ya no existe. Empiece de nuevo.',
        FuncionNoEncontrada: 'Esa función no existe.',
        SinButacas: 'Elija al menos una butaca.'
    };

    function mostrarError(motivo) {
        error.textContent = motivos[motivo] || 'No se pudo completar la operación.';
        error.hidden = false;
    }

    function limpiarError() {
        error.hidden = true;
    }

    function claveDeButaca(fila, numero) {
        return `${fila}${numero}`;
    }

    function pintarSeleccion() {
        for (const elemento of mapa.querySelectorAll('.butaca')) {
            elemento.classList.toggle('mia', mias.has(elemento.dataset.butaca));
        }

        const cantidad = mias.size;
        botonPagar.disabled = cantidad === 0 || pagando;

        if (cantidad === 0) {
            seleccionTexto.textContent = 'Toque las butacas que quiere. Cada una queda apartada 10 minutos.';
            plazo.hidden = true;
            return;
        }

        const total = (cantidad * tarifa).toLocaleString('es-CR', { style: 'currency', currency: 'CRC' });
        seleccionTexto.textContent = `${cantidad} butaca${cantidad === 1 ? '' : 's'}: ${[...mias].join(', ')} — total ${total}`;
        botonPagar.textContent = `Pagar ${total}`;
    }

    function pintarPlazo() {
        if (!venceEn || mias.size === 0) {
            plazo.hidden = true;
            return;
        }

        const faltan = Math.max(0, Math.round((venceEn - Date.now()) / 1000));
        const minutos = String(Math.floor(faltan / 60)).padStart(2, '0');
        const segundos = String(faltan % 60).padStart(2, '0');
        plazo.textContent = `Sus butacas quedan apartadas ${minutos}:${segundos} más.`;
        plazo.hidden = false;

        if (faltan === 0) {
            apartadoId = null;
            venceEn = null;
            mias = new Set();
            pintarSeleccion();
            mostrarError('ApartadoVencido');
        }
    }

    async function pedir(url, cuerpo) {
        const respuesta = await fetch(url, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(cuerpo)
        });

        return { ok: respuesta.ok, datos: await respuesta.json().catch(() => null) };
    }

    async function apartar(fila, numero) {
        const { ok, datos } = await pedir(`/api/funciones/${funcionId}/apartar`, {
            butacas: [{ fila, numero }],
            apartadoId
        });

        if (!ok) {
            mostrarError(datos && datos.motivo);
            if (datos && datos.mapa) {
                dibujar(datos.mapa);
            } else {
                await refrescar();
            }
            return;
        }

        limpiarError();
        apartadoId = datos.apartadoId;
        venceEn = new Date(datos.venceEn).getTime();
        mias.add(claveDeButaca(fila, numero));
        pintarSeleccion();
        pintarPlazo();
    }

    async function liberar(fila, numero) {
        const { ok, datos } = await pedir(`/api/apartados/${apartadoId}/liberar`, { fila, numero });

        if (!ok) {
            mostrarError(datos && datos.motivo);
            return;
        }

        limpiarError();
        mias.delete(claveDeButaca(fila, numero));
        if (mias.size === 0) {
            apartadoId = null;
            venceEn = null;
        }
        pintarSeleccion();
        await refrescar();
    }

    async function pagar() {
        if (mias.size === 0 || pagando) {
            return;
        }

        pagando = true;
        botonPagar.disabled = true;

        // La misma clave en los dos toques deja una sola compra y un solo código (RN-23).
        claveIdempotencia = claveIdempotencia || crypto.randomUUID();

        const butacas = [...mias].map(b => ({ fila: b.slice(0, 1), numero: Number(b.slice(1)) }));
        const { ok, datos } = await pedir(`/api/apartados/${apartadoId}/pagar`, {
            butacas,
            correo: correo.value || null,
            edadDeclarada: false,
            claveIdempotencia
        });

        if (!ok) {
            pagando = false;
            claveIdempotencia = null;
            mostrarError(datos && datos.motivo);
            botonPagar.disabled = false;
            await refrescar();
            return;
        }

        limpiarError();
        document.getElementById('codigo').textContent = datos.codigo;
        document.getElementById('detalleCompra').textContent =
            `${butacas.length} butaca${butacas.length === 1 ? '' : 's'} — total ` +
            datos.total.toLocaleString('es-CR', { style: 'currency', currency: 'CRC' });
        confirmacion.hidden = false;
        panel.hidden = true;
        mias = new Set();
        apartadoId = null;
        venceEn = null;
        await refrescar();
    }

    function dibujar(datos) {
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

        for (const elemento of mapa.querySelectorAll('.butaca')) {
            elemento.classList.toggle('mia', mias.has(elemento.dataset.butaca));
        }
    }

    async function refrescar() {
        try {
            const respuesta = await fetch(`/api/funciones/${funcionId}/mapa`, { cache: 'no-store' });
            if (!respuesta.ok) {
                return;
            }

            dibujar(await respuesta.json());
            aviso.textContent = 'Mapa actualizado a las ' + new Date().toLocaleTimeString('es-CR');
        } catch {
            aviso.textContent = 'No se pudo actualizar el mapa. Se vuelve a intentar en 5 segundos.';
        }
    }

    mapa.addEventListener('click', async function (evento) {
        const elemento = evento.target.closest('.butaca');
        if (!elemento || pagando) {
            return;
        }

        const clave = elemento.dataset.butaca;
        const fila = clave.slice(0, 1);
        const numero = Number(clave.slice(1));

        if (mias.has(clave)) {
            await liberar(fila, numero);
        } else if (elemento.classList.contains('libre')) {
            await apartar(fila, numero);
        } else {
            mostrarError(elemento.classList.contains('novendible') ? 'ButacaNoVendible' : 'ButacaTomada');
        }
    });

    botonPagar.addEventListener('click', pagar);

    setInterval(refrescar, 5000);
    setInterval(pintarPlazo, 1000);
    pintarSeleccion();
})();
