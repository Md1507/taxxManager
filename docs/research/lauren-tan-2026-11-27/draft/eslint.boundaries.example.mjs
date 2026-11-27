// Independent example [c]; provenance and scope are in provenance.md.
// For JavaScript ES-module imports under the two named directories.
export default [
  {
    files: ["src/electron-renderer/**/*.js"],
    rules: {
      "no-restricted-imports": [
        "error",
        {
          paths: [
            {
              name: "@app/main",
              message: "Renderer code must use the shared contract, not the main module.",
            },
          ],
          patterns: [
            {
              group: ["@app/main/**", "**/electron-main", "**/electron-main/**"],
              message: "Move the cross-process contract to shared; keep main implementation outside renderer.",
            },
          ],
        },
      ],
    },
  },
  {
    files: ["src/electron-main/**/*.js"],
    rules: {
      "no-restricted-imports": [
        "error",
        {
          paths: [
            {
              name: "@app/renderer",
              message: "Main code must use the shared contract, not the renderer module.",
            },
          ],
          patterns: [
            {
              group: ["@app/renderer/**", "**/electron-renderer", "**/electron-renderer/**"],
              message: "Move the cross-process contract to shared; keep renderer implementation outside main.",
            },
          ],
        },
      ],
    },
  },
];
