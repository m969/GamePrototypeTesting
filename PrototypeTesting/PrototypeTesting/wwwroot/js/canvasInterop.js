const instances = new Map();
const supportedKeys = new Set(["KeyW", "KeyA", "KeyS", "KeyD", "Space", "ShiftLeft", "ShiftRight"]);

function drawArena(ctx, canvas) {
  const gradient = ctx.createLinearGradient(0, 0, canvas.width, canvas.height);
  gradient.addColorStop(0, "#1a2238");
  gradient.addColorStop(1, "#0c1424");
  ctx.fillStyle = gradient;
  ctx.fillRect(0, 0, canvas.width, canvas.height);

  ctx.strokeStyle = "rgba(255,255,255,0.05)";
  ctx.lineWidth = 1;
  for (let x = 0; x < canvas.width; x += 48) {
    ctx.beginPath();
    ctx.moveTo(x, 0);
    ctx.lineTo(x, canvas.height);
    ctx.stroke();
  }

  for (let y = 0; y < canvas.height; y += 48) {
    ctx.beginPath();
    ctx.moveTo(0, y);
    ctx.lineTo(canvas.width, y);
    ctx.stroke();
  }

  ctx.strokeStyle = "rgba(244, 211, 94, 0.18)";
  ctx.lineWidth = 4;
  ctx.strokeRect(16, 16, canvas.width - 32, canvas.height - 32);
}

function drawEnemies(ctx, snapshot) {
  for (const enemy of snapshot.enemies ?? []) {
    if (!enemy.isAlive) {
      continue;
    }

    ctx.fillStyle = enemy.isHitFlashing ? "#ffd166" : "#ff7a59";
    ctx.beginPath();
    ctx.arc(enemy.x, enemy.y, enemy.radius, 0, Math.PI * 2);
    ctx.fill();

    ctx.fillStyle = "rgba(0,0,0,0.28)";
    ctx.fillRect(enemy.x - enemy.radius, enemy.y - enemy.radius - 10, enemy.radius * 2, 4);
    ctx.fillStyle = "#9be564";
    ctx.fillRect(enemy.x - enemy.radius, enemy.y - enemy.radius - 10, (enemy.health / enemy.maxHealth) * enemy.radius * 2, 4);
  }
}

function drawPlayer(ctx, snapshot) {
  const playerSize = 24;
  const centerX = snapshot.playerX + playerSize / 2;
  const centerY = snapshot.playerY + playerSize / 2;

  if (snapshot.isAttackActive) {
    ctx.save();
    ctx.globalAlpha = 0.28;
    ctx.fillStyle = "#f4d35e";
    ctx.beginPath();
    ctx.arc(centerX + snapshot.playerFacingX * 42, centerY + snapshot.playerFacingY * 42, 38, 0, Math.PI * 2);
    ctx.fill();
    ctx.restore();
  }

  ctx.fillStyle = snapshot.isPlayerHitFlashing ? "#ffb4a2" : snapshot.isPlayerDodging ? "#8ee3ef" : "#f4d35e";
  ctx.fillRect(snapshot.playerX, snapshot.playerY, playerSize, playerSize);

  ctx.strokeStyle = "#0d1321";
  ctx.lineWidth = 3;
  ctx.beginPath();
  ctx.moveTo(centerX, centerY);
  ctx.lineTo(centerX + snapshot.playerFacingX * 18, centerY + snapshot.playerFacingY * 18);
  ctx.stroke();
}

function drawHud(ctx, canvas, snapshot) {
  ctx.fillStyle = "#e6edf7";
  ctx.font = "16px Segoe UI";
  ctx.fillText(`HP ${snapshot.playerHealth}/${snapshot.maxPlayerHealth}`, 24, 32);
  ctx.fillText(`Enemies ${snapshot.enemiesRemaining}`, 24, 56);
  ctx.fillText(`Kills ${snapshot.kills}`, 24, 80);
  ctx.fillText(`Attacks ${snapshot.attacks}  Dodges ${snapshot.dodges}`, 24, 104);
  ctx.fillText(`Time ${snapshot.elapsedSeconds.toFixed(1)}s`, 24, 128);

  ctx.fillStyle = "rgba(255,255,255,0.72)";
  ctx.fillText("WASD move  Space slash  Shift dodge", 24, canvas.height - 24);

  if (!snapshot.isBattleOver) {
    return;
  }

  ctx.save();
  ctx.fillStyle = "rgba(7, 10, 17, 0.62)";
  ctx.fillRect(0, 0, canvas.width, canvas.height);
  ctx.fillStyle = snapshot.outcome === "Victory" ? "#9be564" : "#ff9b85";
  ctx.font = "bold 42px Segoe UI";
  ctx.textAlign = "center";
  ctx.fillText(snapshot.outcome, canvas.width / 2, canvas.height / 2 - 12);
  ctx.fillStyle = "#e6edf7";
  ctx.font = "18px Segoe UI";
  ctx.fillText("填写右侧反馈或直接保存结果", canvas.width / 2, canvas.height / 2 + 24);
  ctx.restore();
}

function bindInput(instance) {
  const { canvas, dotNet } = instance;

  instance.onKeyDown = (event) => {
    if (!supportedKeys.has(event.code)) {
      return;
    }

    event.preventDefault();
    dotNet.invokeMethodAsync("HandleInputDown", event.code);
  };

  instance.onKeyUp = (event) => {
    if (!supportedKeys.has(event.code)) {
      return;
    }

    dotNet.invokeMethodAsync("HandleInputUp", event.code);
  };

  instance.onPointerDown = () => {
    canvas.focus();
    dotNet.invokeMethodAsync("HandleCanvasFocused");
  };

  canvas.addEventListener("pointerdown", instance.onPointerDown);
  window.addEventListener("keydown", instance.onKeyDown);
  window.addEventListener("keyup", instance.onKeyUp);
}

function unbindInput(instance) {
  const { canvas } = instance;
  canvas.removeEventListener("pointerdown", instance.onPointerDown);
  window.removeEventListener("keydown", instance.onKeyDown);
  window.removeEventListener("keyup", instance.onKeyUp);
}

export function initializePrototypeCanvas(canvas, dotNet, options) {
  disposePrototypeCanvas(canvas);

  canvas.width = options.width;
  canvas.height = options.height;

  const instance = {
    canvas,
    dotNet,
    ctx: canvas.getContext("2d")
  };

  bindInput(instance);
  canvas.focus();
  instances.set(canvas, instance);
}

export function renderPrototypeCanvas(canvas, snapshot) {
  const instance = instances.get(canvas);
  if (!instance) {
    return;
  }

  const { ctx } = instance;
  drawArena(ctx, canvas);
  drawEnemies(ctx, snapshot);
  drawPlayer(ctx, snapshot);
  drawHud(ctx, canvas, snapshot);
}

export function disposePrototypeCanvas(canvas) {
  const instance = instances.get(canvas);
  if (!instance) {
    return;
  }

  unbindInput(instance);
  instances.delete(canvas);
}

window.prototypeTestingStorage = {
  load(key) {
    return window.localStorage.getItem(key);
  },
  save(key, value) {
    window.localStorage.setItem(key, value);
  }
};
