#!/bin/bash

# E2E Test Runner for Tour Planner (Team 03)
# Usage: ./run_tests.sh [API_URL]
# Default API URL: http://localhost:8080

API_URL="${1:-http://localhost:8080}"
export TOUR_PLANNER_API_URL="$API_URL"

echo "============================================="
echo "Tour Planner (Team 03) E2E Test Runner"
echo "Target API: $API_URL"
echo "============================================="

# 1. Check if the API is up
echo "Checking backend API availability..."
UP=false
for i in {1..10}; do
  if curl -s -f "$API_URL/api/user" > /dev/null; then
    UP=true
    break
  fi
  echo "Backend API is not ready yet. Retrying in 2 seconds... ($i/10)"
  sleep 2
done

if [ "$UP" = false ]; then
  echo "Error: Backend API at $API_URL/api/user is not reachable."
  echo "Please start the backend services (e.g., using docker-compose up) before running tests."
  exit 1
fi
echo "Backend API is UP and running!"

# 2. Reset the database
echo ""
echo "---------------------------------------------"
echo "Database Reset Procedure:"
echo "To ensure clean tests, you can truncate the database tables."
echo "If running with docker-compose, run:"
echo "  docker-compose exec -T db psql -U postgres -d tour_planner -c \"TRUNCATE app_user CASCADE;\""
echo "Otherwise, execute:"
echo "  TRUNCATE app_user, tour, tour_waypoint, tour_log CASCADE;"
echo "---------------------------------------------"

# Attempt to automatically truncate using docker-compose if db service is active
if command -v docker-compose &> /dev/null && docker-compose ps | grep -q "db"; then
  echo "Docker-compose detected. Attempting automatic table truncation..."
  docker-compose exec -T db psql -U postgres -d tour_planner -c "TRUNCATE app_user CASCADE;" &>/dev/null
  if [ $? -eq 0 ]; then
    echo "Database successfully truncated!"
  else
    echo "Automatic truncation skipped (database might be busy or not fully initialized)."
  fi
else
  echo "Automatic truncation skipped (docker-compose or db service not found)."
fi

# 3. Run the python E2E tests
echo ""
echo "Running E2E tests..."
python3 tests/e2e/test_e2e.py
TEST_EXIT_CODE=$?

echo "============================================="
echo "Tests finished with exit code: $TEST_EXIT_CODE"
echo "============================================="
exit $TEST_EXIT_CODE
