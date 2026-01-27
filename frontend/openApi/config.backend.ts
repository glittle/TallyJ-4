import { defineConfig } from "@hey-api/openapi-ts";

export default defineConfig({
  input: {
    path: "C:/Users/glenl/.zenflow/worktrees/jan-26-b-57ab/frontend/openApi/tallyj.json",
  },
  output: {
    path: "src/api/gen/configService",
    clean: true,
  },
  plugins: [
    "@hey-api/schemas",
    {
      dates: true,
      name: "@hey-api/transformers",
    },
    {
      enums: "javascript",
      name: "@hey-api/typescript",
    },
    {
      name: "@hey-api/sdk",
      transformer: true,
    },
  ],
});
