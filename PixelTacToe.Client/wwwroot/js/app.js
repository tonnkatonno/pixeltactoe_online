const API_BASE   = 'http://109.122.217.232:5246';
const defaultImg = '/img/default-player.png';

const preview     = document.getElementById('preview');
const fileInput   = document.getElementById('fileInput');
const nameInput   = document.getElementById('nameInput');
const createBtn   = document.getElementById('createBtn');
const statusLbl   = document.getElementById('status');
const lobbyUl     = document.getElementById('lobbyList');
const requestUl   = document.getElementById('requestList');
const leadersUl   = document.getElementById('leaders');

(function preloadForm(){
  const storedName = localStorage.getItem('lastName');
  const storedImg  = localStorage.getItem('lastImg');
  if (storedName) nameInput.value = storedName;
  if (storedImg)  preview.src     = storedImg;
})();

let myId = sessionStorage.getItem('myId') || null;
let pollId = null;
let heartbeatId = null;
const lobbyCache = new Map();

fileInput.addEventListener('change', e => {
  const f = e.target.files[0];
  if (!f) return;
  const r = new FileReader();
  r.onload = ev => (preview.src = ev.target.result);
  r.readAsDataURL(f);
});

createBtn.addEventListener('click', async () => {
  const name = nameInput.value.trim();
  const file = fileInput.files[0];
  if (!name) { statusLbl.textContent = '⚠️ name required'; return; }
  if (!file) { statusLbl.textContent = '⚠️ choose image'; return; }

  const resizedFile = await resizeImageFile(file, 256, 256);

  const fd = new FormData();
  fd.append('name', name);
  fd.append('file', resizedFile);

  try {
    statusLbl.textContent = 'uploading…';
    const res  = await fetch(`${API_BASE}/api/CreatePlayer/CreatePlayer`,
        { method:'POST', body:fd });
    if (!res.ok) throw new Error(await res.text());

    const json = await res.json();
    myId       = json.createdPlayerId;
    sessionStorage.setItem('myId', myId);
    preview.src = API_BASE + json.url;
    localStorage.setItem('lastName', name);
    localStorage.setItem('lastImg',  API_BASE + json.url);
    statusLbl.textContent = '✅ uploaded';

    await refreshLobby();
    await refreshRequests();

    if (!pollId) pollId = setInterval(() => {
      refreshLobby();
      refreshRequests();
    }, 2500);

  } catch (err) {
    statusLbl.textContent = '❌ ' + err.message;
    console.error(err);
  }
});

async function resizeImageFile(file, width, height) {
  return new Promise((resolve, reject) => {
    const img = new Image();
    img.onload = () => {
      const canvas = document.createElement('canvas');
      canvas.width = width;
      canvas.height = height;
      const ctx = canvas.getContext('2d');
      ctx.drawImage(img, 0, 0, width, height);

      let outType = file.type;
      let ext = '';
      switch (file.type) {
        case "image/png": ext = '.png'; break;
        case "image/jpeg": ext = '.jpg'; break;
        case "image/gif": ext = '.gif'; break;
        case "image/webp": ext = '.webp'; break;
        default:
          outType = "image/png";
          ext = '.png';
          break;
      }

      canvas.toBlob(blob => {
        if (blob) {
          const baseName = file.name.replace(/\.[^/.]+$/, "");
          const newFile = new File([blob], baseName + ext, { type: outType });
          resolve(newFile);
        } else {
          reject(new Error("Failed to resize image"));
        }
      }, outType);
    };
    img.onerror = reject;
    const reader = new FileReader();
    reader.onload = e => { img.src = e.target.result; };
    reader.onerror = reject;
    reader.readAsDataURL(file);
  });
}

async function refreshLobby () {
  if (!myId || !lobbyUl) return;

  try {
    const res  = await fetch(`${API_BASE}/api/Players/players/lobby`);
    if (!res.ok) return;
    const list = await res.json();

    lobbyUl.innerHTML = '';
    lobbyCache.clear();
    list.forEach(p => lobbyCache.set(p.id, p));

    list.filter(p => p.id !== myId).forEach(p => {
      const li = document.createElement('li');
      li.textContent = p.name;
      li.style.cursor = 'pointer';
      li.title = 'Send game request';
      li.addEventListener('click', () => sendMatchRequest(p.id));
      lobbyUl.appendChild(li);
    });

  } catch (e) { console.error(e); }
}

async function sendMatchRequest (targetId) {
  try {
    const res = await fetch(
        `${API_BASE}/api/Players/${targetId}/matchrequest`, {
          method: 'POST',
          headers: { 'Content-Type':'application/json' },
          body: JSON.stringify(myId)
        });
    if (res.ok) {
      waitForGameStart();
    }

  } catch (e) { console.error(e); }
}

async function waitForGameStart () {
  const poll = setInterval(async () => {
    try {
      const res = await fetch(`${API_BASE}/uploads/${myId}.json`,
          { cache: 'no-cache' });
      if (!res.ok) return;

      const meta = await res.json();

      if (meta.CurrentGameId) {
        clearInterval(poll);
        location.href = `/game.html?id=${meta.CurrentGameId}`;
      }

    } catch (e) {
      console.error('waitForGameStart', e);
    }
  }, 2500);
}

async function refreshRequests () {
  if (!myId || !requestUl) return;

  try {
    const res = await fetch(`${API_BASE}/api/Players/${myId}/matchrequests`);
    if (!res.ok) return;
    const ids = await res.json();

    requestUl.innerHTML = '';
    ids.forEach(id => {
      const li  = document.createElement('li');
      li.textContent = lobbyCache.get(id)?.name || id;

      const btn = document.createElement('button');
      btn.textContent = 'Accept';
      btn.className   = 'btn btn-primary btn-sm';
      btn.style.marginLeft = '8px';
      btn.onclick = () => acceptRequest(id);

      li.appendChild(btn);
      requestUl.appendChild(li);
    });

  } catch (e) { console.error(e); }
}

async function acceptRequest (targetId) {
  try {
    const res = await fetch(
        `${API_BASE}/api/Players/${myId}/acceptmatchrequest`, {
          method:'POST',
          headers:{ 'Content-Type':'application/json' },
          body: JSON.stringify(targetId)
        });
    if (res.ok) {
      const { gameId } = await res.json();
      statusLbl.textContent = `✅ game ${gameId} started`;
      requestUl.innerHTML  = '';
      location.href = `/game.html?id=${gameId}`;
    } else {
      statusLbl.textContent = '❌ accept failed';
    }
  } catch (e) { console.error(e); }
}

async function refreshLeaderboard () {
  try {
    const res = await fetch(`${API_BASE}/api/Players/players/leaderboard`);
    if (!res.ok) return;
    const list = await res.json();

    leadersUl.innerHTML = '';
    list.forEach(p=>{
      const li = document.createElement('li');
      li.innerHTML =
          `<img src="${API_BASE + p.imageUrl}" alt="">`
          + `<span>${p.name}</span>`
          + `<small>(${p.wins}-${p.losses}-${p.draws})</small>`;
      leadersUl.appendChild(li);
    });
  }catch(e){ console.error(e); }
}

setInterval(refreshLeaderboard, 5000);
refreshLeaderboard();

(async function initAfterReload(){
  if (!myId) return;

  await refreshLobby();
  await refreshRequests();

  if (!pollId)
    pollId = setInterval(() => {
      refreshLobby();
      refreshRequests();
    }, 2500);

  try{
    const res = await fetch(`${API_BASE}/uploads/${myId}.json`,
        { cache:'no-cache' });
    if (res.ok){
      const meta = await res.json();
      if (meta.CurrentGameId){
        location.href = `/game.html?id=${meta.CurrentGameId}`;
      }
    }
  }catch(e){ console.error(e); }

  function sendHeartbeat() {
    if (!myId) return;
    fetch(`${API_BASE}/api/Players/${myId}/heartbeat`, { method: 'POST' })
        .catch(console.error);
  }

  sendHeartbeat();
  heartbeatId = setInterval(sendHeartbeat, 10000);

  window.addEventListener('beforeunload', () => {
    clearInterval(heartbeatId);
    navigator.sendBeacon(
        `${API_BASE}/api/Players/${myId}/heartbeat`
    );
  });
})();
