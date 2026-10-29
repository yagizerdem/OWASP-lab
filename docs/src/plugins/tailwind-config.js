const path = require("path");

module.exports = function tailwindPlugin(context, options) {
  return {
    name: "tailwind-plugin",
    configurePostCss(postcssOptions) {
      postcssOptions.plugins = [require("@tailwindcss/postcss")];
      return postcssOptions;
    },
    configureWebpack() {
      return {
        resolve: {
          alias: {
            "@": path.resolve(context.siteDir, "src"),
          },
        },
      };
    },
  };
};
