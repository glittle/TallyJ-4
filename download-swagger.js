const https = require('http');
const fs = require('fs');

const url = 'http://localhost:5016/swagger/v1/swagger.json';

https.get(url, (res) => {
  let data = '';

  res.on('data', (chunk) => {
    data += chunk;
  });

  res.on('end', () => {
    fs.writeFileSync('frontend/openApi/tallyj.json', data);
    console.log('Downloaded swagger.json');
  });
}).on('error', (err) => {
  console.error('Error:', err);
});