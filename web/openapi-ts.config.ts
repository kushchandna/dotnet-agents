import { defineConfig } from '@hey-api/openapi-ts';

export default defineConfig({
  client: '@hey-api/client-fetch',
  input: 'http://localhost:5000/openapi/v1.json',
  output: { path: 'src/api/generated', format: 'prettier' }
});
