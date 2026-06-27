#!/bin/bash

# ==============================================================================
# Tour Planner (Team 03) - Unified E2E Test Runner
# ==============================================================================
# This script performs the following tasks:
# 1. Resets the PostgreSQL database (clears and truncates all tables and resets sequences).
# 2. Ensures the backend API is up and running.
# 3. Runs the E2E test suite.
# 4. Reports the results and exits with the correct status code.
# ==============================================================================

# Exit immediately if a command exits with a non-zero status
set -e

# Configuration
API_URL="${TOUR_PLANNER_API_URL:-http://localhost:8080}"
export TOUR_PLANNER_API_URL="$API_URL"

echo "========================================================================"
echo " Starting Tour Planner E2E Test Runner"
echo " Target API: $API_URL"
echo "========================================================================"

# --- Step 1: Database Reset Mechanism ---
echo ""
echo "[1/3] Resetting the database..."

DB_RESET_SUCCESS=false

# Try Method A: docker-compose (hyphenated)
if command -v docker-compose &> /dev/null; then
  echo "Attempting database reset via docker-compose..."
  if docker-compose exec -T db psql -U postgres -d tour_planner -c "TRUNCATE tour_waypoint, tour_log, tour, app_user RESTART IDENTITY CASCADE;" &> /dev/null; then
    echo "Database reset successful via docker-compose exec."
    DB_RESET_SUCCESS=true
  fi
fi

# Try Method B: docker compose (space-separated)
if [ "$DB_RESET_SUCCESS" = false ] && command -v docker &> /dev/null; then
  echo "Attempting database reset via docker compose..."
  if docker compose exec -T db psql -U postgres -d tour_planner -c "TRUNCATE tour_waypoint, tour_log, tour, app_user RESTART IDENTITY CASCADE;" &> /dev/null; then
    echo "Database reset successful via docker compose exec."
    DB_RESET_SUCCESS=true
  fi
fi

# Try Method C: docker exec using container name/id search
if [ "$DB_RESET_SUCCESS" = false ] && command -v docker &> /dev/null; then
  echo "Attempting database reset via direct docker exec..."
  DB_CONTAINER=$(docker ps -q -f name=db | head -n 1 || true)
  if [ -n "$DB_CONTAINER" ]; then
    if docker exec -i "$DB_CONTAINER" psql -U postgres -d tour_planner -c "TRUNCATE tour_waypoint, tour_log, tour, app_user RESTART IDENTITY CASCADE;" &> /dev/null; then
      echo "Database reset successful via docker exec."
      DB_RESET_SUCCESS=true
    fi
  fi
fi

# Try Method D: local psql command line
if [ "$DB_RESET_SUCCESS" = false ]; then
  echo "Attempting database reset via local psql client..."
  if PGPASSWORD="${PGPASSWORD:-password}" psql -h localhost -p "${PGPORT:-5432}" -U "${PGUSER:-postgres}" -d "${PGDATABASE:-tour_planner}" -c "TRUNCATE tour_waypoint, tour_log, tour, app_user RESTART IDENTITY CASCADE;" &> /dev/null; then
    echo "Database reset successful via local psql."
    DB_RESET_SUCCESS=true
  fi
fi

if [ "$DB_RESET_SUCCESS" = false ]; then
  echo "Database reset skipped or could not connect to database."
  echo "Please make sure the PostgreSQL container or local instance is running on port 5432."
else
  echo "Database initialized and ready."
fi

# --- Step 2: Health Check ---
echo ""
echo "[2/3] Checking backend API availability at $API_URL..."
API_READY=false
for i in {1..15}; do
  # Try contacting the user endpoint (returns 200 or similar if up)
  if curl -s -f -o /dev/null "$API_URL/api/user" || curl -s -o /dev/null -w "%{http_code}" "$API_URL/api/user" | grep -q -E "^(200|404|400|401)$"; then
    API_READY=true
    break
  fi
  echo "Backend API is not ready yet. Retrying in 2 seconds... ($i/15)"
  sleep 2
done

if [ "$API_READY" = false ]; then
  echo "Error: Backend API at $API_URL is not reachable."
  echo "Please start the backend services before running tests."
  exit 1
fi
echo "Backend API is UP and running!"

# --- Step 3: Execute Tests ---
echo ""
echo "[3/3] Running E2E Test Suite..."
set +e # Do not exit immediately on test failures so we can report them
python3 tests/e2e/test_e2e.py
TEST_EXIT_CODE=$?
set -e

echo ""
echo "========================================================================"
if [ $TEST_EXIT_CODE -eq 0 ]; then
  echo "SUCCESS: All E2E tests passed!"
else
  echo "FAILURE: E2E tests failed with exit code $TEST_EXIT_CODE"
fi
echo "========================================================================"

exit $TEST_EXIT_CODE
