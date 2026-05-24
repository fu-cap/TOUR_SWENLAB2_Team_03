# TOUR_SWENLAB2

This repository represent the semester group project for the tour planer in SWEN LAB 2. 


### Run the project (Full Docker)

```bash
docker compose up --build
```

This starts three services:
- **Frontend** → http://localhost:80
- **Backend** → http://localhost:8080
- **Database** → PostgreSQL on port 5432

---

### Local Development Workflow (Recommended)

For a better development experience (Hot Module Replacement for the Frontend), it is recommended to run the Database and Backend in Docker, but the Frontend locally.

#### 1. Start Database and Backend
```bash
docker compose up db backend
```

#### 2. Start Frontend locally
Open a new terminal and run:
```bash
cd tour-planner-frontend
npm install
npm start
```
- **Local Frontend** → http://localhost:4200

#### 3. Start Backend locally (Optional)
If you want to debug the C# code, run only the database in Docker:
```bash
docker compose up db
```
Then open `TourPlanner.slnx` in Visual Studio or JetBrains Rider and run the `TourPlanner.API` project.
- **Backend API** → http://localhost:8080 (or as configured in your IDE)

---

### Stop the project

```bash
docker compose down
```

> **Note:** If changes are not reflected, force a clean rebuild with:
> ```bash
> docker compose build --no-cache && docker compose up
> ```

