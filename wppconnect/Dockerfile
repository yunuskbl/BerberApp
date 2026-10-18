FROM node:20-bullseye-slim

# Puppeteer/Chromium sistem bağımlılıkları
RUN apt-get update && apt-get install -y \
    git \
    chromium \
    ca-certificates \
    fonts-liberation \
    libgbm1 \
    --no-install-recommends \
    && rm -rf /var/lib/apt/lists/*

ENV PUPPETEER_SKIP_CHROMIUM_DOWNLOAD=true \
    PUPPETEER_EXECUTABLE_PATH=/usr/bin/chromium

# Çalışma dizini. DİKKAT: wppconnect-server oturum credential'larını çalışma
# dizinine göre ./tokens altına yazar, yani /app/tokens. docker-compose.yml'deki
# wppconnect_tokens volume'ü bu yola mount edilmeli — aksi halde oturumlar
# konteynerin yazılabilir katmanında kalır ve her yeniden yaratmada silinir.
WORKDIR /app

# Hangi sürümün derleneceği. Varsayılan "main" mevcut davranışı korur; çalışan
# bir sürüm doğrulandığında .env içinden WPPCONNECT_REF ile bir etikete veya
# commit SHA'sına sabitlenmelidir:
#   WPPCONNECT_REF=v2.10.4      (etiket)
#   WPPCONNECT_REF=9f3c1ab...   (commit)
# Pinlenmediğinde her rebuild farklı bir upstream sürümü getirir; WhatsApp Web
# uyumsuzlukları (gönderimde TypeError) sisteme bu yoldan habersiz girer.
ARG WPPCONNECT_REF=main

RUN git clone https://github.com/wppconnect-team/wppconnect-server.git . \
    && git checkout --detach "${WPPCONNECT_REF}" \
    && git rev-parse HEAD > /app/.wppconnect-build-ref \
    && echo "wppconnect-server derlenen commit: $(cat /app/.wppconnect-build-ref)"

RUN npm install
RUN npm run build

EXPOSE 21465

CMD ["node", "dist/server.js"]
