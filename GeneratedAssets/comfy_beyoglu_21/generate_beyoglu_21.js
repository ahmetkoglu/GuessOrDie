const fs = require('fs');
const path = require('path');
const http = require('http');

const COMFY = 'http://127.0.0.1:8188';
const workDir = path.resolve(__dirname);
const sourcePath = path.join(workDir, 'source_pera_hill.jpg');
const outputPath = path.join(workDir, 'beyoglu_21_comfy.png');

function request(method, urlPath, { headers = {}, body = null } = {}) {
  return new Promise((resolve, reject) => {
    const url = new URL(urlPath, COMFY);
    const req = http.request(url, { method, headers }, (res) => {
      const chunks = [];
      res.on('data', (chunk) => chunks.push(chunk));
      res.on('end', () => {
        const buffer = Buffer.concat(chunks);
        if (res.statusCode < 200 || res.statusCode >= 300) {
          reject(new Error(`${method} ${urlPath} failed: ${res.statusCode} ${buffer.toString('utf8', 0, 500)}`));
          return;
        }
        resolve({ status: res.statusCode, headers: res.headers, buffer });
      });
    });
    req.on('error', reject);
    if (body) req.write(body);
    req.end();
  });
}

async function uploadImage() {
  const boundary = '----guessordie-comfy-boundary-' + Date.now();
  const file = fs.readFileSync(sourcePath);
  const parts = [
    Buffer.from(`--${boundary}\r\nContent-Disposition: form-data; name="image"; filename="source_pera_hill.jpg"\r\nContent-Type: image/jpeg\r\n\r\n`),
    file,
    Buffer.from(`\r\n--${boundary}\r\nContent-Disposition: form-data; name="overwrite"\r\n\r\ntrue\r\n--${boundary}--\r\n`),
  ];
  const body = Buffer.concat(parts);
  const response = await request('POST', '/upload/image', {
    headers: { 'Content-Type': `multipart/form-data; boundary=${boundary}`, 'Content-Length': body.length },
    body,
  });
  return JSON.parse(response.buffer.toString('utf8')).name || 'source_pera_hill.jpg';
}

async function queuePrompt(uploadedName) {
  const clientId = 'guessordie-beyoglu-21-' + Date.now();
  const prompt = {
    '1': { class_type: 'CheckpointLoaderSimple', inputs: { ckpt_name: 'meinapastel_v6Pastel.safetensors' } },
    '10': { class_type: 'LoraLoader', inputs: { model: ['1', 0], clip: ['1', 1], lora_name: 'Scenery_enchancer-Anima-P3.safetensors', strength_model: 0.85, strength_clip: 0.85 } },
    '2': { class_type: 'CLIPTextEncode', inputs: { clip: ['10', 1], text: 'Pera Hill Beyoglu Istanbul urban street view inspired by the reference image, hillside hotel and apartment facade composition, narrow street perspective, historic Beyoglu architecture, balconies and windows simplified, all brand names removed, no readable signs, no text, soft pastel color palette, warm and calm atmosphere, simplified geometric shapes, soft gradients, minimal texture, storybook illustration style, modern flat illustration with subtle depth, rounded forms, clean edges, soft lighting, slightly desaturated colors, cozy nostalgic mood, balanced composition, no harsh shadows, vector-like but painterly, high quality, detailed but minimal, 4k illustration, square 1:1 composition, mobile game background art' } },
    '3': { class_type: 'CLIPTextEncode', inputs: { clip: ['10', 1], text: 'brand name, hotel name, Pera Hill text, storefront sign, signage, readable text, letters, words, logo, watermark, caption, typography, photorealistic, realistic photo, harsh shadow, sharp shadow, noisy texture, cluttered details, oversaturated colors, dark gloomy atmosphere, low quality, blurry, distorted buildings, deformed perspective, people closeup, cars closeup' } },
    '4': { class_type: 'LoadImage', inputs: { image: uploadedName } },
    '5': { class_type: 'ImageScale', inputs: { image: ['4', 0], upscale_method: 'lanczos', width: 1024, height: 1024, crop: 'center' } },
    '6': { class_type: 'VAEEncode', inputs: { pixels: ['5', 0], vae: ['1', 2] } },
    '7': { class_type: 'KSampler', inputs: { model: ['10', 0], positive: ['2', 0], negative: ['3', 0], latent_image: ['6', 0], seed: 210724, steps: 32, cfg: 7.5, sampler_name: 'euler', scheduler: 'normal', denoise: 0.66 } },
    '8': { class_type: 'VAEDecode', inputs: { samples: ['7', 0], vae: ['1', 2] } },
    '9': { class_type: 'SaveImage', inputs: { images: ['8', 0], filename_prefix: 'guessordie_beyoglu_21' } },
  };
  const body = Buffer.from(JSON.stringify({ prompt, client_id: clientId }));
  const response = await request('POST', '/prompt', { headers: { 'Content-Type': 'application/json', 'Content-Length': body.length }, body });
  return JSON.parse(response.buffer.toString('utf8')).prompt_id;
}

async function getJson(urlPath) {
  const response = await request('GET', urlPath);
  return JSON.parse(response.buffer.toString('utf8'));
}

async function waitForOutput(promptId) {
  for (let i = 0; i < 240; i++) {
    const history = await getJson(`/history/${promptId}`);
    const item = history[promptId];
    const images = item?.outputs?.['9']?.images;
    if (images && images.length) return images[0];
    await new Promise((resolve) => setTimeout(resolve, 1000));
  }
  throw new Error('Timed out waiting for ComfyUI output');
}

async function downloadImage(image) {
  const params = new URLSearchParams({ filename: image.filename, subfolder: image.subfolder || '', type: image.type || 'output' });
  const response = await request('GET', `/view?${params.toString()}`);
  fs.writeFileSync(outputPath, response.buffer);
  return outputPath;
}

(async () => {
  if (!fs.existsSync(sourcePath)) throw new Error(`Missing source image: ${sourcePath}`);
  const uploadedName = await uploadImage();
  console.log('UPLOADED', uploadedName);
  const promptId = await queuePrompt(uploadedName);
  console.log('PROMPT_ID', promptId);
  const image = await waitForOutput(promptId);
  console.log('OUTPUT', JSON.stringify(image));
  const saved = await downloadImage(image);
  const buffer = fs.readFileSync(saved);
  console.log('SAVED', saved, `${buffer.readUInt32BE(16)}x${buffer.readUInt32BE(20)}`, buffer.length);
})().catch((error) => {
  console.error(error.stack || error.message);
  process.exit(1);
});