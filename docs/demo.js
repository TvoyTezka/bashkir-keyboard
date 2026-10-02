(() => {
  const output = document.getElementById('try-output');
  if (!output) return;
  let text = '';
  let active = null;
  const render = () => { output.textContent = text || 'Здесь появятся буквы'; };
  function cancel() {
    if (!active) return;
    clearTimeout(active.timer);
    active.button.classList.remove('holding');
    active = null;
  }
  function begin(button, upper) {
    cancel();
    text = text.slice(-199);
    text += upper ? button.dataset.letter.toUpperCase() : button.dataset.letter;
    render();
    const replacement = upper ? button.dataset.hold.toUpperCase() : button.dataset.hold;
    const candidate = { button, timer: null };
    active = candidate;
    button.classList.add('holding');
    candidate.timer = setTimeout(() => {
      if (active !== candidate) return;
      text = text.slice(0, -1) + replacement;
      render();
      button.classList.remove('holding');
    }, 450);
  }
  document.querySelectorAll('[data-letter]').forEach(button => {
    let suppressClick = false;
    button.addEventListener('pointerdown', event => {
      if (event.button !== 0 || !event.isPrimary) return;
      event.preventDefault();
      button.focus();
      suppressClick = true;
      button.setPointerCapture(event.pointerId);
      begin(button, event.shiftKey);
    });
    const end = () => { if (active?.button === button) cancel(); };
    button.addEventListener('pointerup', end);
    button.addEventListener('pointercancel', end);
    button.addEventListener('lostpointercapture', end);
    button.addEventListener('blur', end);
    button.addEventListener('keydown', event => {
      if (event.key !== ' ' && event.key !== 'Enter') return;
      event.preventDefault();
      if (event.repeat) return;
      suppressClick = true;
      begin(button, event.shiftKey);
    });
    button.addEventListener('keyup', event => {
      if (event.key === ' ' || event.key === 'Enter') {
        event.preventDefault();
        end();
        suppressClick = false;
      }
    });
    button.addEventListener('click', () => {
      if (suppressClick) { suppressClick = false; return; }
      begin(button, false);
      cancel();
    });
  });
  document.getElementById('try-clear').addEventListener('click', () => {
    cancel(); text = ''; render();
  });
  window.addEventListener('blur', cancel);
  document.addEventListener('visibilitychange', () => { if (document.hidden) cancel(); });
})();
