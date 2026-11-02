const ctx = new AudioContext();
const master = ctx.createGain();
master.connect(ctx.destination);
const sfxBus = ctx.createGain();
sfxBus.connect(master);
const musicBus = ctx.createGain();
musicBus.connect(master);
const buses = {master, sfx: sfxBus, music: musicBus};
const sfxLoops = new Map();
const loopTokens = new Map();

const cache = new Map();
let music = null;
let musicToken = 0;

const analysers = new Map();
const vis = new Map();
let visId = 0;

const unlock = () => {
    if (ctx.state !== "running") ctx.resume();
};
["pointerdown", "keydown", "touchstart"].forEach(e => addEventListener(e, unlock, {once: true}));

function get(url) {
    let p = cache.get(url);
    if (!p) {
        p = fetch(url)
            .then(r => {
                if (!r.ok) throw new Error(`${r.status} ${url}`);
                return r.arrayBuffer();
            })
            .then(b => ctx.decodeAudioData(b)
                .catch(err => console.error(`${err} ${url}`)));
        p.catch(() => cache.delete(url));
        cache.set(url, p);
    }
    return p;
}

function analyser(bus) {
    let a = analysers.get(bus);
    if (!a) {
        const shelf = ctx.createBiquadFilter();
        shelf.type = "lowshelf";
        shelf.frequency.value = 150;
        shelf.gain.value = -12;
        
        a = ctx.createAnalyser();
        a.fftSize = 512;
        a.smoothingTimeConstant = 0;
        a.minDecibels = -55;
        a.maxDecibels = -10;
        buses[bus].connect(shelf).connect(a);
        analysers.set(bus, a);
    }
    return a;
}

export async function preload(urls) {
    await Promise.all(urls.map(get));
}

export async function sfx(url, volume = 1, rate = 1) {
    const buf = await get(url);
    const src = ctx.createBufferSource();
    src.buffer = buf;
    src.playbackRate.value = rate;
    const g = ctx.createGain();
    g.gain.value = volume;
    src.connect(g).connect(sfxBus);
    src.start();
}

export async function playMusic(url, volume = 1, fade = 1, loop = true) {
    if (music?.url === url) return;
    const id = ++musicToken;
    const buf = await get(url);
    if (id !== musicToken) return;
    stopMusic(fade);
    const t = ctx.currentTime;
    const src = ctx.createBufferSource();
    src.buffer = buf;
    src.loop = loop;
    const g = ctx.createGain();
    g.gain.setValueAtTime(0, t);
    g.gain.linearRampToValueAtTime(volume, t + fade);
    src.connect(g).connect(musicBus);
    src.start();
    music = {src, g, url};
}

export function stopMusic(fade = 1) {
    if (!music) return;
    const {src, g} = music;
    music = null;
    const t = ctx.currentTime;
    g.gain.cancelScheduledValues(t);
    g.gain.setValueAtTime(g.gain.value, t);
    g.gain.linearRampToValueAtTime(0, t + fade);
    src.stop(t + fade);
}

function fadeOut({src, g}, fade) {
    const t = ctx.currentTime;
    g.gain.cancelScheduledValues(t);
    g.gain.setValueAtTime(g.gain.value, t);
    g.gain.linearRampToValueAtTime(0, t + fade);
    src.stop(t + fade);
}

export async function sfxLoop(key, url, volume = 1, rate = 1, fade = 0) {
    if (sfxLoops.has(key)) return;
    const token = (loopTokens.get(key) ?? 0) + 1;
    loopTokens.set(key, token);
    const buf = await get(url);
    if (loopTokens.get(key) !== token || sfxLoops.has(key)) return;
    const t = ctx.currentTime;
    const src = ctx.createBufferSource();
    src.buffer = buf;
    src.loop = true;
    src.playbackRate.value = rate;
    const g = ctx.createGain();
    g.gain.setValueAtTime(fade > 0 ? 0 : volume, t);
    if (fade > 0) g.gain.linearRampToValueAtTime(volume, t + fade);
    src.connect(g).connect(sfxBus);
    src.start();
    sfxLoops.set(key, {src, g});
}

export function stopSfx(key, fade = 0) {
    loopTokens.set(key, (loopTokens.get(key) ?? 0) + 1);
    const loop = sfxLoops.get(key);
    if (!loop) return;
    sfxLoops.delete(key);
    fadeOut(loop, fade);
}

export function stopAllSfx(fade = 0) {
    for (const key of [...loopTokens.keys()]) stopSfx(key, fade);
}

export function isSfxLooping(key) {
    return sfxLoops.has(key);
}

export function setVolume(bus, v) {
    buses[bus].gain.value = v;
}

export function startVisualizer(canvas, bus = "master", mode = "bars", opts = {}) {
    const id = ++visId;
    const a = analyser(bus);
    const g = canvas.getContext("2d");
    const freq = new Uint8Array(a.frequencyBinCount);
    const wave = new Uint8Array(a.fftSize);
    const {bars = 64, color = "#7CFC00", bg = null, attack = 1, release = 0.15} = opts;
    const level = new Float32Array(bars);

    const draw = () => {
        const v = vis.get(id);
        if (!v) return;
        const dpr = devicePixelRatio || 1;
        const w = canvas.clientWidth * dpr, h = canvas.clientHeight * dpr;
        if (canvas.width !== w || canvas.height !== h) {
            canvas.width = w;
            canvas.height = h;
        }
        g.clearRect(0, 0, w, h);
        if (bg) {
            g.fillStyle = bg;
            g.fillRect(0, 0, w, h);
        }

        if (mode === "wave") {
            a.getByteTimeDomainData(wave);
            g.strokeStyle = color;
            g.lineWidth = 2 * dpr;
            g.beginPath();
            for (let i = 0; i < wave.length; i++) {
                const x = i / (wave.length - 1) * w, y = wave[i] / 255 * h;
                i ? g.lineTo(x, y) : g.moveTo(x, y);
            }
            g.stroke();
        } else {
            a.getByteFrequencyData(freq);
            const n = freq.length, bw = w / bars;
            g.fillStyle = color;
            for (let i = 0; i < bars; i++) {
                const skip = 4;
                const lo = skip + Math.floor(Math.pow(n - skip, i / bars));
                const hi = Math.max(lo + 1, skip + Math.floor(Math.pow(n - skip, (i + 1) / bars)));
                let m = 0;
                for (let j = lo; j < hi && j < n; j++) if (freq[j] > m) m = freq[j];
                const target = m / 255;
                const k = target > level[i] ? attack : release;
                level[i] += (target - level[i]) * k;
                const bh = level[i] * h;
                g.fillRect(i * bw + 1, h - bh, bw - 2, bh);
            }
        }
        v.raf = requestAnimationFrame(draw);
    };

    vis.set(id, {raf: requestAnimationFrame(draw)});
    return id;
}

export function stopVisualizer(id) {
    const v = vis.get(id);
    if (!v) return;
    cancelAnimationFrame(v.raf);
    vis.delete(id);
}