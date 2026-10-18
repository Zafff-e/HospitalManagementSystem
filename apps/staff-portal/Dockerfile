# Multi-stage Dockerfile for Angular Staff Portal
FROM node:24-alpine AS build
WORKDIR /app

# Copy package descriptors
COPY package*.json ./
RUN npm ci

# Copy source
COPY . .

# Build application
RUN npm run build -- --configuration production

# Production serving with Nginx
FROM nginx:alpine
COPY --from=build /app/dist/staff-portal /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
