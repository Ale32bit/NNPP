const VS = `attribute vec2 p; void main(){ gl_Position = vec4(p,0.,1.); }`;
const FS = `
precision highp float;
uniform float t, dose, px, fps;
float h(vec3 p){
  p = fract(p * .1031);
  p += dot(p, p.zyx + 31.32);
  return fract((p.x + p.y) * p.z);
}
void main(){
  vec2 cell = floor(gl_FragCoord.xy / px);
  float f = mod(floor(t * fps), 997.);
  float r = h(vec3(cell, f));
  float hit = step(1. - dose, r) * (.6 + .4 * h(vec3(cell, f + 3.)));
  float grain = h(vec3(cell + 17., f)) * .06;
  float a = max(hit, grain);
  gl_FragColor = vec4(vec3(a), a);
}`;

export function start(canvas, opts) {
    const gl = canvas.getContext('webgl', {premultipliedAlpha: true});
    const sh = (type, src) => {
        const s = gl.createShader(type);
        gl.shaderSource(s, src);
        gl.compileShader(s);
        return s;
    };
    const pr = gl.createProgram();
    gl.attachShader(pr, sh(gl.VERTEX_SHADER, VS));
    gl.attachShader(pr, sh(gl.FRAGMENT_SHADER, FS));
    gl.linkProgram(pr);
    gl.useProgram(pr);

    gl.bindBuffer(gl.ARRAY_BUFFER, gl.createBuffer());
    gl.bufferData(gl.ARRAY_BUFFER, new Float32Array([-1, -1, 3, -1, -1, 3]), gl.STATIC_DRAW);
    const loc = gl.getAttribLocation(pr, 'p');
    gl.enableVertexAttribArray(loc);
    gl.vertexAttribPointer(loc, 2, gl.FLOAT, false, 0, 0);

    const u = n => gl.getUniformLocation(pr, n);
    const uT = u('t'), uDose = u('dose'), uPx = u('px'), uFps = u('fps');
    const o = {dose: 0.002, px: 3, fps: 24, ...opts};

    const size = () => {
        canvas.width = canvas.clientWidth;
        canvas.height = canvas.clientHeight;
        gl.viewport(0, 0, canvas.width, canvas.height);
    };
    addEventListener('resize', size);
    size();

    let raf;
    const frame = ms => {
        gl.uniform1f(uT, ms / 1000);
        gl.uniform1f(uDose, o.dose);
        gl.uniform1f(uPx, o.px);
        gl.uniform1f(uFps, o.fps);
        gl.drawArrays(gl.TRIANGLES, 0, 3);
        raf = requestAnimationFrame(frame);
    };
    raf = requestAnimationFrame(frame);

    return {
        set(next) {
            Object.assign(o, next);
        },
        stop() {
            cancelAnimationFrame(raf);
            removeEventListener('resize', size);
            gl.getExtension('WEBGL_lose_context')?.loseContext();
        }
    };
}