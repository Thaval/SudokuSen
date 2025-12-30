import os, math, wave, struct, random

ROOT = r"C:\\Users\\dave\\source\\repos\\MySudoku"
AUDIO_DIR = os.path.join(ROOT, "Audio")
os.makedirs(AUDIO_DIR, exist_ok=True)

SR = 44100

def write_wav_mono(path, samples):
    with wave.open(path, 'wb') as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(SR)
        frames = bytearray()
        for s in samples:
            s = max(-1.0, min(1.0, float(s)))
            frames += struct.pack('<h', int(s * 32767))
        w.writeframes(frames)

def env_exp(i, n, power=2.0):
    x = 1.0 - (i / n)
    return max(0.0, x) ** power

def click(freq=1200.0, dur=0.045, amp=0.50):
    """Percussive click: damped tone + short noise burst (more tactile than a pure sine)."""
    n = int(SR * dur)
    for i in range(n):
        t = i / SR
        p = i / n
        e = env_exp(i, n, power=3.2)

        # Damped tone
        tone = math.sin(2 * math.pi * freq * t)
        tone += 0.35 * math.sin(2 * math.pi * freq * 2.0 * t)

        # Very short noise burst (strong at the start, quickly decays)
        noise_amt = 0.22 * (1.0 - p) ** 5
        noise = (random.random() * 2 - 1) * noise_amt

        s = 0.75 * tone + noise
        yield s * e * amp

def tone(freq=523.25, dur=0.10, amp=0.30, bend=0.0):
    n = int(SR * dur)
    for i in range(n):
        t = i / SR
        p = i / n
        f = freq * (1.0 + bend * (p - 0.5))
        if p < 0.08:
            e = p / 0.08
        elif p < 0.25:
            e = 1.0 - (p - 0.08) * 1.2
        else:
            e = max(0.0, 0.7 * (1.0 - (p - 0.25) / 0.75))
        s = math.sin(2*math.pi*f*t)*0.7 + math.sin(2*math.pi*f*2*t)*0.3
        yield s * e * amp

def error_buzz(dur=0.20, amp=0.30):
    n = int(SR * dur)
    for i in range(n):
        t = i / SR
        p = i / n
        e = 1.0 - p
        s = (math.sin(2*math.pi*200*t)*0.45 + math.sin(2*math.pi*250*t)*0.35 + math.sin(2*math.pi*180*t)*0.20)
        s += (random.random()*2-1) * 0.05
        yield s * e * amp

def success_chime(dur=0.25, amp=0.28):
    n = int(SR * dur)
    f1, f2 = 523.25, 659.25
    for i in range(n):
        t = i / SR
        p = i / n
        f = f1 if p < 0.40 else f2
        pp = (p / 0.40) if p < 0.40 else ((p - 0.40) / 0.60)
        e = (pp/0.08) if pp < 0.08 else max(0.0, 1.0 - (pp - 0.08)/0.92)
        s = math.sin(2*math.pi*f*t)*0.75 + math.sin(2*math.pi*f*2*t)*0.25
        yield s * e * amp

def win_fanfare(dur=0.60, amp=0.28):
    n = int(SR * dur)
    freqs = [523.25, 659.25, 783.99]
    for i in range(n):
        t = i / SR
        p = i / n
        if p < 0.25:
            f = freqs[0]; pp = p/0.25
        elif p < 0.50:
            f = freqs[1]; pp = (p-0.25)/0.25
        else:
            f = freqs[2]; pp = (p-0.50)/0.50
        e = (pp/0.06) if pp < 0.06 else max(0.0, 1.0 - (pp-0.06)/0.94)
        if p >= 0.50:
            e = max(e, 0.25*(1.0-pp))
        s = (math.sin(2*math.pi*f*t)*0.60 + math.sin(2*math.pi*f*2*t)*0.25 + math.sin(2*math.pi*f*0.5*t)*0.15)
        yield s * e * amp

def clamp01(x):
    return 0.0 if x < 0.0 else (1.0 if x > 1.0 else x)

def softclip(x):
    # gentle saturation
    return math.tanh(1.6 * x)

def pad_music(loop_seconds=12.0, base_amp=0.18, mood="menu"):
    """Simple loopable ambient pad made from integer-frequency sines (seamless looping)."""
    n = int(SR * loop_seconds)
    # Choose integer frequencies so freq * loop_seconds is an integer (seamless loop)
    if mood == "menu":
        freqs = [220, 264, 330, 440]  # A3, ~C4, E4, A4
        lfo_rate = 1.0 / 6.0
    else:
        freqs = [196, 247, 294, 392]  # G3, ~B3, D4, G4
        lfo_rate = 1.0 / 4.0

    for i in range(n):
        t = i / SR
        # Periodic envelope (same at start/end)
        lfo = 0.85 + 0.15 * math.sin(2 * math.pi * lfo_rate * t)

        s = 0.0
        for k, f in enumerate(freqs):
            # Slightly detune upper partials for warmth (still integer-ish: keep detune tiny)
            det = 1.0 + (0.0009 if k % 2 == 0 else -0.0007)
            s += (1.0 / (k + 1.2)) * math.sin(2 * math.pi * (f * det) * t)
            s += 0.22 * (1.0 / (k + 1.6)) * math.sin(2 * math.pi * (f * 2.0) * t)

        s = softclip(s * base_amp * lfo)
        yield s

files = {
    "click.wav": list(click(1200, 0.045)),
    "cell_select.wav": list(click(900, 0.040, amp=0.45)),
    "number_place.wav": list(tone(523.25, 0.10, bend=0.35)),
    "number_remove.wav": list(tone(392.00, 0.08, bend=-0.25)),
    "note_place.wav": list(click(1500, 0.030, amp=0.42)),
    "note_remove.wav": list(click(700, 0.030, amp=0.40)),
    "error.wav": list(error_buzz(0.20)),
    "success.wav": list(success_chime(0.25)),
    "win.wav": list(win_fanfare(0.60)),

    # Background music (loopable)
    "menu_music.wav": list(pad_music(loop_seconds=12.0, base_amp=0.18, mood="menu")),
    "game_music.wav": list(pad_music(loop_seconds=12.0, base_amp=0.20, mood="game")),
}

for name, samples in files.items():
    path = os.path.join(AUDIO_DIR, name)
    write_wav_mono(path, samples)
    print(f"wrote {name}")