let resizingElement = null;
let startX = 0;
let startLeftWidth = 0;
let startRightWidth = 0;

export function initPrepareCvResizers() {
  const gutters = document.querySelectorAll('.gutter');
  gutters.forEach(function(gutter) {
    if (gutter.dataset.initialized === 'true') {
      return;
    }
    gutter.dataset.initialized = 'true';
    gutter.onmousedown = null;
    gutter.addEventListener('mousedown', onMouseDown);
  });
}

export function onMouseDown(e) {
  e.preventDefault();
  resizingElement = e.currentTarget;
  startX = e.clientX;
  const leftPanel = resizingElement.previousElementSibling;
  const rightPanel = resizingElement.nextElementSibling;
  startLeftWidth = leftPanel.offsetWidth;
  startRightWidth = rightPanel.offsetWidth;
  document.addEventListener('mousemove', onMouseMove);
  document.addEventListener('mouseup', onMouseUp);
  resizingElement.parentElement.classList.add('resizing');
}

export function onMouseMove(e) {
  if (!resizingElement)
      return;

  const dx = e.clientX - startX;
  const newLeftWidth = Math.max(200, Math.min(600, startLeftWidth + dx));
  const newRightWidth = Math.max(200, Math.min(600, startRightWidth - dx));
  resizingElement.previousElementSibling.style.flexBasis = newLeftWidth + 'px';
  resizingElement.nextElementSibling.style.flexBasis = newRightWidth + 'px';
}

export function onMouseUp() {
  document.removeEventListener('mousemove', onMouseMove);
  document.removeEventListener('mouseup', onMouseUp);
  if (resizingElement) {
    resizingElement.parentElement.classList.remove('resizing');
  }
  resizingElement = null;
}

window.initPrepareCvResizers = initPrepareCvResizers;
