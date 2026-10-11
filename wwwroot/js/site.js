// Autocompletado de usuario/libro (RNF01: préstamo en pocos pasos)
document.querySelectorAll('input[data-lookup]').forEach(inp => {
  const dl = document.getElementById(inp.getAttribute('list')); let t;
  inp.addEventListener('input', () => {
    clearTimeout(t); const q = inp.value.trim(); if (q.length < 2) return;
    t = setTimeout(async () => {
      const r = await fetch(inp.dataset.lookup + '?term=' + encodeURIComponent(q));
      const d = await r.json(); dl.innerHTML = '';
      d.forEach(x => { const o = document.createElement('option'); o.value = x.value; o.label = x.label; dl.appendChild(o); });
    }, 200);
  });
});
