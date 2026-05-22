# Chess Roguelike PvP - AI-Driven Telemetry & Balancing System
> [!CAUTION]
The live cloud environment (Render/PostgreSQL) was decommissioned after the successful thesis defense. The repository serves as an architectural showcase and codebase reference.

## 📌 Overview
An automated game balancing ecosystem built for a multiplayer turn-based chess strategy. This project solves the problem of subjective game balancing by implementing a complete data pipeline: from in-game telemetry collection to an automated AI-driven analysis loop that generates intelligent configuration patches.

## 🏗️ Architecture & Tech Stack
The system is designed with a decoupled client-server architecture:
- **Game Client (Unity 6 / C#)**: Generates gameplay events and manages local telemetry buffering.
- **Backend API (ASP.NET Core 8)**: RESTful API acting as a secure gateway for telemetry ingestion.
- **Database (PostgreSQL + EF Core)**: Relational storage tracking game sessions, complex events, and versioned economy configs.
- **Automation & AI (n8n + Google Gemini)**: Analyzes telemetry KPI metrics and suggests JSON balance modifications.
- **Deployment**: Hosted on Render using environment variables for secret management. 

![Architecture Diagram]
<img width="944" height="169" alt="image" src="https://github.com/user-attachments/assets/2914002b-31d4-4720-8464-085b8deda19f" />

## 🚀 Key Features
- **Resilient Data Collection**: The Unity client uses batched event sending with an offline queue and retry policies to prevent data loss during network drops. 
- **Idempotent API**: The ASP.NET Core backend handles network retransmissions gracefully. It tracks unique `eventId`s to guarantee data integrity and prevent duplicate records in the database. 
- **Automated AI Feedback Loop**: An integrated n8n webhook triggers the Google Gemini model. It consumes aggregated match statistics (WinRates, unit popularity, economy flow) and outputs a ready-to-deploy, modified JSON game configuration.
- **Interactive Analytics Dashboard**: A custom UI for monitoring real-time game metrics, player placement heatmaps, and evaluating AI balance recommendations.

## 📸 Screenshots

### 🎮 The Game (Data Generator)
* **Lobby:** Lobby, allows you to create, search and join players.
<img width="584" height="493" alt="image" src="https://github.com/user-attachments/assets/8a83dc74-4347-428d-bbc3-76bff10ca6bf" />

* **Shop Phase:** Economic decisions (buying/placing units) that generate crucial telemetry events for the AI to analyze.
<img width="854" height="504" alt="image" src="https://github.com/user-attachments/assets/7c8c9bfd-1c5b-486f-9cb9-2d22040907c9" />

* **Battle Phase:** Turn-based combat where unit interactions, win/loss metrics, and player strategies are logged in real-time.
<img width="445" height="626" alt="image" src="https://github.com/user-attachments/assets/2fd4f5c0-ae27-435d-b687-bd881ec7a023" />

* **Analytics Dashboard**: View KPI metrics and unit statistics.
<img width="945" height="450" alt="image" src="https://github.com/user-attachments/assets/afbbd5f6-96f2-4a20-b667-6dc59ccbd4c9" />

* **AI Recommendation Engine**: Comparing active config against LLM-generated JSON suggestions.
<img width="945" height="502" alt="image" src="https://github.com/user-attachments/assets/52151d3e-5f22-4423-b3dc-a3564792ff4f" />

* **n8n Automation Flow**: The data pipeline connecting the database to Google Gemini.
<img width="945" height="327" alt="image" src="https://github.com/user-attachments/assets/76eb6b40-0b8d-4114-a5a5-ea1795f4a88b" />

## ⚙️ Setup & Installation

**Backend:**
1. Configure `appsettings.json` or environment variables for `DATABASE_URL`.
2. Run EF Core migrations to build the PostgreSQL schema.
3. Start the ASP.NET Core application.

**Unity Client:**
1. Open the project in Unity 6.
2. Locate the `TelemetryConfig` asset in the Inspector and update it with your running backend URL.
<img width="400" alt="Telemetry Config" src="[LINK_DO_SCREENA_Z_TELEMETRY_CONFIG]">
3. Build and run the project.

## 👨‍💻 Author
**Karol Jabłoński** - [www.linkedin.com/in/karol-jabłoński00](https://www.linkedin.com/in/karol-jabłoński00)
