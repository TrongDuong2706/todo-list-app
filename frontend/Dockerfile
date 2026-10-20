FROM node:22-alpine AS build
WORKDIR /app
COPY package*.json ./
RUN npm ci
COPY . .
RUN npm run build

FROM nginx:stable-alpine
# The official Nginx image renders files in /etc/nginx/templates at startup.
# API_UPSTREAM can therefore be set to a Kubernetes Service without rebuilding.
COPY nginx.conf /etc/nginx/templates/default.conf.template
COPY --from=build /app/dist /usr/share/nginx/html
ENV API_UPSTREAM=http://backend:8080
EXPOSE 80
