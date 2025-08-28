# Trading-website
A simple **SPA** built with **Nuxt + Vue 3 (frontend)** and **.NET 9 (backend)** that visualizes a real-time **order book chart** (bids in green, asks in red). Data is streamed via **WebSockets** and displayed with **D3.js**.

---

## ✨ Features
- 📊 Interactive bar chart with bids/asks  
- 🎚️ Adjustable number of entries displayed  
- 📈 Price range dynamically updates when entries change  
- 🔄 Live updates via WebSocket  
- 🖱️ Tooltips on hover for quick info  
- ⚡ Fast and lightweight SPA setup  
- 🖥️ Backend in .NET with WebSocket broadcasting  

### Order book chart example:
<img width="2559" height="1216" alt="Screenshot 2025-08-28 155153" src="https://github.com/user-attachments/assets/a994ec7d-c24b-4597-b336-5f319c9d41f5" />

### Audit log example:
<img width="2559" height="1218" alt="Screenshot 2025-08-28 155209" src="https://github.com/user-attachments/assets/30f96768-17d5-41e5-98f0-19aa69bab1fd" />

---

## 🛠️ Tech Stack
- **Frontend:** Nuxt + Vue 3 + D3.js  
- **Backend:** ASP.NET Core (.NET 9) + WebSockets  

## ⚙️ Installation

### 1. Clone the Repository
```sh
git clone git@github.com:mshchygol/trading-website.git
cd trading-website
```

### 2. Backend Setup (.NET)
```sh
cd backend
dotnet restore
dotnet run
```

### 3. Frontend Setup (Nuxt + Vue 3)
```sh
cd application
npm install
npm run dev
```
