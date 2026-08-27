/** @type {import('next').NextConfig} */
const nextConfig = {
  experimental: {
    serverComponentsExternalPackages: ["ssh2", "bcryptjs"],
  },
};

export default nextConfig;
