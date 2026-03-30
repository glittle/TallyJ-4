const fs = require('fs');
const path = require('path');

const dir = path.join(__dirname, 'dist', 'assets');
const files = fs.readdirSync(dir);
const jsFile = files.find(f => f.startsWith('admin-layout') && f.endsWith('.js'));

const code = fs.readFileSync(path.join(dir, jsFile), 'utf8');
console.log('Size:', code.length);

const chunks = code.split('},{');
console.log('Number of split parts by "},{":', chunks.length);

// What is the biggest string literal or base64?
let longestString = '';
const stringRegex = /"([^"\\]|\\.)*"/g;
let match;
let i = 0;
while ((match = stringRegex.exec(code)) !== null) {
  if (match[0].length > longestString.length) {
    longestString = match[0];
  }
  i++;
}
console.log(`Found ${i} strings. Longest string length: ${longestString.length}`);

// check where the bulk of the content is by dividing it into 100kb segments
for(let offset=0; offset < code.length; offset += 100000) {
    console.log(`Offset ${offset}: ${code.substring(offset, offset + 100).replace(/\n/g, '')}`);
}
