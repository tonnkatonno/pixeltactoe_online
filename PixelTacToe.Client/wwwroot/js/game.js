const API  = 'http://localhost:5246';

const gameId = new URLSearchParams(location.search).get('id');
if (!gameId) { alert('missing gameId'); throw 'no id'; }

const boardDiv = document.getElementById('board');
const infoLbl  = document.getElementById('info');
const myId     = sessionStorage.getItem('myId') || '';

let state  = null;
let p1img, p2img;

for (let i = 0; i < 9; i++) {
    const c = document.createElement('div');
    c.className   = 'cell';
    c.dataset.idx = i;
    c.onclick = () => tryMove(i);
    boardDiv.appendChild(c);
}

async function fetchState() {
    const res = await fetch(`${API}/api/Game/${gameId}`);
    if (!res.ok) { console.error('state fetch', res.status); return; }
    state = await res.json();
    if (!p1img) await cacheAvatars();
    render();
}

async function cacheAvatars(){
    p1img = new Image(); p1img.src = API + state.player1Img;
    p2img = new Image(); p2img.src = API + state.player2Img;
}

function defaultPlaceholder() {
    return '/img/default-player.png';
}

function render() {
    const b = state.state.board;
    const finished = state.state.finished;
    
    const boardArr = (b.length === 0) ? Array(9).fill('') : b;

    Array.from(boardDiv.children).forEach((c, i) => {
        const mark = boardArr[i];
        c.innerHTML = '';
        if (mark === 'X')      c.appendChild(p1img.cloneNode());
        else if (mark === 'O') c.appendChild(p2img.cloneNode());
        c.classList.toggle('disabled', !!mark || finished);
    });

    if (finished) {
        infoLbl.textContent =
            state.state.winner === 'draw' ? 'Draw!' :
                state.state.winner === myId   ? 'You won! 🎉' : 'You lost 😞';
    } else {
        infoLbl.textContent =
            state.state.nextPlayer === myId ? 'Your turn' : 'Waiting for opponent…';
    }
    if (state.state.finished) {
        infoLbl.innerHTML =
            state.state.winner === 'draw'
                ? 'Draw! <button id="again">play again</button>'
                : (state.state.winner === myId
                    ? 'You won! 🎉 <button id="again">play again</button>'
                    : 'You lost 😞 <button id="again">play again</button>');
        document.getElementById('again').onclick = () => {
            location.href = '/';
        };
    }
}

async function tryMove(idx) {
    if (state.state.finished) return;
    if (state.state.nextPlayer !== myId) return;
    if ((state.state.board[idx] || '') !== '') return;

    const res = await fetch(`${API}/api/Game/${gameId}/move`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ playerId: myId, cell: idx })
    });
    if (res.ok) await fetchState();
    else alert(await res.text());
}

fetchState();
const poll = setInterval(() => {
    if (state?.state.finished) clearInterval(poll);
    else fetchState();
}, 2500);