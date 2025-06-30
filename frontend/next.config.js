module.exports = {
  reactStrictMode: true,
  images: {
    domains: ['kaspi.kz', 'resources.cdn-kaspi.kz'], // Add Kaspi.kz image domains
  },
  env: {
    API_URL: process.env.API_URL || 'http://localhost:5000/api', // Set your API URL here
  },
  webpack: (config) => {
    config.module.rules.push({
      test: /\.svg$/,
      use: ['@svgr/webpack'],
    });
    return config;
  },
};