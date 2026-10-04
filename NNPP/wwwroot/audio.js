const ctx = new AudioContext();
const master = ctx.createGain(); master.connect(ctx.destination);
const sfxBus = ctx.createGain(); sfxBus.connect(master);
const musicBus = ctx.createGain(); musicBus.connect(master);
const buses = { master, sfx: sfxBus, music: musicBus };

const cache = new Map();
let music = null;
let musicToken = 0;

const unlock = () => { if (ctx.state !== "running") ctx.resume(); };
["pointerdown", "keydown", "touchstart"].forEach(e => addEventListener(e, unlock, { once: true }));

function get(url) {
    let p = cache.get(url);
    if (!p) {
        p = fetch(url)
            .then(r => { if (!r.ok) throw new Error(`${r.status} ${url}`); return r.arrayBuffer(); })
            .then(b => ctx.decodeAudioData(b));
        p.catch(() => cache.delete(url));
        cache.set(url, p);
    }
    return p;
}

export async function preload(urls) { await Promise.all(urls.map(get)); }

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
    music = { src, g, url };
}

export function stopMusic(fade = 1) {
    if (!music) return;
    const { src, g } = music;
    music = null;
    const t = ctx.currentTime;
    g.gain.cancelScheduledValues(t);
    g.gain.setValueAtTime(g.gain.value, t);
    g.gain.linearRampToValueAtTime(0, t + fade);
    src.stop(t + fade);
}

export function setVolume(bus, v) { buses[bus].gain.value = v; }