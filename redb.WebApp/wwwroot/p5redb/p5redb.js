let layer;
let blur;
let img;

function setup() {
    createCanvas(256, 140, WEBGL);
    img = loadImage('dB.png');
    layer = createFramebuffer();
    blur = createShader(vert, frag);
    noStroke();
}

function draw() {
    layer.begin();
    let locX = mouseX - width / 2;
    let locY = mouseY - height / 2;
    background('rgba(234,239,241, 0.2)');
    pointLight(230, 230, 230, locX, locY, 150);
    orbitControl();
    ambientLight(150);
    directionalLight(255, 255, 255, -1, 1, -1);
    ambientMaterial(255, 0, 0);
    fill(255, 255, 100);
    specularMaterial(255);
    shininess(150);

    rotateY(millis() * 0.0005);
    rotateX(0.1);

    push();
    //fill('orange');
    texture(img);
    translate(40 * sin(millis() / 1000), 40 * cos(millis() / 1000), 50 * sin(millis() / 800 + 500));
    sphere(10, 15, 15);
    pop();

    push();
    texture(img);
    translate(40 * cos(millis() / 1000), 40 * sin(millis() / 1000), 50 * cos(millis() / 800 + 500));
    box(10, 15, 15);
    pop();

    push();
    texture(img);
    translate(40 * cos(millis() / 1000), 40 * sin(millis() / 1000), 50 * sin(millis() / 800 + 500));
    cylinder(10, 15, 15);
    pop();


    for (let i = 0; i < 3; i++) {
        push();
        translate(-0, (i - 2) * 15);
        cylinder(20, 12);
        pop();
    }
    layer.end();

    // Render the scene with depth of field blur
    shader(blur);
    blur.setUniform('img', layer.color);
    blur.setUniform('depth', layer.depth);
    rect(0, 0, width, height);
}

function windowResized() {
    resizeCanvas(windowWidth, windowHeight);
}

let vert = `
precision highp float;
attribute vec3 aPosition;
attribute vec2 aTexCoord;
varying vec2 vTexCoord;
void main() {
  vec4 positionVec4 = vec4(aPosition, 1.0);
  positionVec4.xy = positionVec4.xy * 2.0 - 1.0;
  positionVec4.y *= -.8;
  gl_Position = positionVec4;
  vTexCoord = aTexCoord;
}`;

let frag = `
precision highp float;
varying vec2 vTexCoord;
uniform sampler2D img;
uniform sampler2D depth;
float getBlurriness(float d) {
  // Blur more the farther away we go from the
  // focal point at depth=0.9
  return abs(d - 0.9) * 40.;
}
float maxBlurDistance(float blurriness) {
  return blurriness * 0.02;
}
void main() {
  vec4 color = texture2D(img, vTexCoord);
  float samples = 1.;
  float centerDepth = texture2D(depth, vTexCoord).r;
  float blurriness = getBlurriness(centerDepth);
  for (int sample = 0; sample < 20; sample++) {
    // Sample nearby pixels in a spiral going out from the
    // current pixel
    float angle = float(sample);
    float distance = float(sample)/20.
      * maxBlurDistance(blurriness);
    vec2 offset = vec2(cos(angle), sin(angle)) * distance;

    // How close is the object at the nearby pixel?
    float sampleDepth = texture2D(depth, vTexCoord + offset).r;

    // How far should its blur reach?
    float sampleBlurDistance =
      maxBlurDistance(getBlurriness(sampleDepth));

    // If it's in front of the current pixel, or its blur overlaps
    // with the current pixel, add its color to the average
    if (
      sampleDepth >= centerDepth ||
      sampleBlurDistance >= distance
    ) {
      color += texture2D(img, vTexCoord + offset);
      samples++;
    }
  }
  color /= samples;
  gl_FragColor = color;
}`;
