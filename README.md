# TOUR_SWENLAB2

This repository represent the semester group project for the tour planer in SWEN LAB 2. 


### Run the project

```bash
docker compose up --build
```

This starts three services:
- **Frontend** → http://localhost:80
- **Backend** → http://localhost:8080
- **Database** → PostgreSQL on port 5432

### Stop the project

```bash
docker compose down
```

> **Note:** If changes are not reflected, force a clean rebuild with:
> ```bash
> docker compose build --no-cache && docker compose up
> ```

