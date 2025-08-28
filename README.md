# Trading-website
A simple **SPA** built with **Nuxt + Vue 3 (frontend)** and **.NET 9 (backend)** that visualizes a real-time **order book chart** (bids in green, asks in red). Data is streamed via **WebSockets** and displayed with **D3.js**.

---

## ✨ Features
- 📊 Interactive bar chart with bids/asks  
- 🔄 Live updates via WebSocket  
- 🖱️ Tooltips on hover for quick info  
- ⚡ Fast and lightweight SPA setup  
- 🖥️ Backend in .NET with WebSocket broadcasting  

### Order book chart example:
<img width="2557" height="1234" alt="Screenshot 2025-08-26 180808" src="https://github.com/user-attachments/assets/1e84b088-c383-48d4-9890-d94e0ada9837" />

### Audit log example:
<img width="2559" height="1230" alt="Screenshot 2025-08-26 180951" src="https://github.com/user-attachments/assets/78d063da-41d2-4656-a802-fe725c00723d" />

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
