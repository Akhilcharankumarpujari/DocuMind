# ==============================================================================
# DocuMind AI - Frontend Production Multi-Stage Dockerfile
# Stage 1: Build React 19 + Vite application
# Stage 2: Production Nginx Server with SPA fallback & API reverse proxy
# ==============================================================================

# Stage 1: Build React 19 + Vite Application
FROM node:22-alpine AS build
WORKDIR /app

# Copy dependency manifests
COPY package.json package-lock.json ./

# Install exact dependencies
RUN npm ci

# Copy all source files
COPY . .

# Set empty API base URL so the built bundle uses relative /api endpoints
ENV VITE_API_BASE_URL=""

# Build production assets into /app/dist
RUN npm run build

# Stage 2: Production Nginx Alpine Runtime
FROM nginx:alpine AS runtime

# Remove default website
RUN rm -rf /usr/share/nginx/html/*

# Copy compiled assets from build stage
COPY --from=build /app/dist /usr/share/nginx/html

# Copy custom Nginx configuration (SPA fallback + SSE reverse proxy)
COPY nginx.conf /etc/nginx/conf.d/default.conf

# Expose HTTP port
EXPOSE 80

# Run Nginx in foreground
CMD ["nginx", "-g", "daemon off;"]
