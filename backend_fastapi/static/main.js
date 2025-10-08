async function checkHealth() {
  const statusEl = document.getElementById('status');
  try {
    const res = await fetch('/health');
    if (!res.ok) throw new Error('Network response was not ok');
    const data = await res.json();
    statusEl.textContent = 'Status: ' + (data.status || JSON.stringify(data));
  } catch (err) {
    statusEl.textContent = 'Error: ' + err.message;
  }
}

window.addEventListener('load', () => {
  checkHealth();
});
