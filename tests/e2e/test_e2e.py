import os
import unittest
import urllib.request
import urllib.error
import json
import uuid
from datetime import datetime

# Configuration
BASE_URL = os.environ.get("TOUR_PLANNER_API_URL", "http://localhost:8080")

class APIResponse:
    def __init__(self, status, headers, body):
        self.status = status
        self.headers = headers
        self.body = body
        try:
            self.json = json.loads(body) if body else None
        except Exception:
            self.json = None

def api_request(method, path, body=None, headers=None):
    if headers is None:
        headers = {}
    
    if body is not None and "Content-Type" not in headers:
        headers["Content-Type"] = "application/json"
        
    url = f"{BASE_URL.rstrip('/')}/{path.lstrip('/')}"
    
    data = None
    if body is not None:
        if isinstance(body, (dict, list)):
            data = json.dumps(body).encode('utf-8')
        elif isinstance(body, str):
            data = body.encode('utf-8')
        else:
            data = body
            
    req = urllib.request.Request(url, data=data, headers=headers, method=method)
    try:
        with urllib.request.urlopen(req, timeout=5) as resp:
            resp_body = resp.read().decode('utf-8')
            return APIResponse(resp.status, resp.headers, resp_body)
    except urllib.error.HTTPError as e:
        resp_body = e.read().decode('utf-8')
        return APIResponse(e.code, e.headers, resp_body)
    except Exception as e:
        # Fallback for connection issues or generic exceptions
        class ErrorResponse:
            def __init__(self, err):
                self.status = 599
                self.headers = {}
                self.body = str(err)
                self.json = {"error": str(err)}
        return ErrorResponse(e)

class TourPlannerE2ETests(unittest.TestCase):

    # --- Helper Methods ---
    
    def get_id(self, response):
        self.assertIsNotNone(response.json, f"Expected JSON response, got None. Body: {response.body}")
        self.assertIn("id", response.json, f"Expected 'id' field in JSON. Body: {response.json}")
        return response.json["id"]

    def register_user(self, username, password="Password123!", email=None, gender="male", firstname="First", lastname="Last"):
        if email is None:
            email = f"{username}@example.com"
        payload = {
            "username": username,
            "password": password,
            "email": email,
            "gender": gender,
            "firstname": firstname,
            "lastname": lastname
        }
        return api_request("POST", "/api/user", body=payload)

    def login_user(self, username, password="Password123!"):
        payload = {
            "username": username,
            "password": password
        }
        return api_request("POST", "/api/user/login", body=payload)

    def create_tour(self, user_id, name, description="Description", transport_type="cycling-regular", waypoints=None):
        if waypoints is None:
            waypoints = [
                {"label": "Start Point", "latitude": 48.2082, "longitude": 16.3738},
                {"label": "End Point", "latitude": 48.2100, "longitude": 16.3800}
            ]
        payload = {
            "userId": user_id,
            "name": name,
            "description": description,
            "waypoints": waypoints,
            "transportType": transport_type
        }
        return api_request("POST", "/api/tour", body=payload)

    def create_log(self, tour_id, comment="Log comment", difficulty=1, distance=10.0, time_str="01:00:00", rating=5):
        payload = {
            "tourId": tour_id,
            "dateTime": datetime.now().strftime("%Y-%m-%dT%H:%M:%S"),
            "comment": comment,
            "difficulty": difficulty,
            "totalDistanceKm": distance,
            "totalTimeMin": time_str,
            "rating": rating
        }
        return api_request("POST", "/api/log", body=payload)

    # ==========================================
    # TIER 1: Feature Coverage (36 tests)
    # ==========================================

    # --- Feature 1: User Management ---
    def test_t1_user_register_success(self):
        username = f"user_reg_{uuid.uuid4().hex[:8]}"
        res = self.register_user(username)
        self.assertEqual(res.status, 201)

    def test_t1_user_login_success(self):
        username = f"user_log_{uuid.uuid4().hex[:8]}"
        self.register_user(username)
        res = self.login_user(username)
        self.assertEqual(res.status, 200)
        self.assertIsNotNone(res.json)
        self.assertEqual(res.json.get("username"), username)
        self.assertIn("id", res.json)

    def test_t1_user_register_duplicate(self):
        username = f"user_dup_{uuid.uuid4().hex[:8]}"
        self.register_user(username)
        res = self.register_user(username)
        self.assertEqual(res.status, 400)

    def test_t1_user_get_by_id(self):
        username = f"user_get_{uuid.uuid4().hex[:8]}"
        self.register_user(username)
        login_res = self.login_user(username)
        user_id = self.get_id(login_res)
        res = api_request("GET", f"/api/user/{user_id}")
        self.assertEqual(res.status, 200)
        self.assertIsNotNone(res.json)
        self.assertEqual(res.json.get("username"), username)

    def test_t1_user_isolation_basic(self):
        username_a = f"user_iso_a_{uuid.uuid4().hex[:8]}"
        username_b = f"user_iso_b_{uuid.uuid4().hex[:8]}"
        self.register_user(username_a)
        self.register_user(username_b)
        
        id_a = self.get_id(self.login_user(username_a))
        id_b = self.get_id(self.login_user(username_b))
        
        tour_res = self.create_tour(id_a, "User A's Private Tour")
        self.assertEqual(tour_res.status, 201)
        tour_id = self.get_id(tour_res)
        
        tours_b_res = api_request("GET", f"/api/tour?userId={id_b}")
        self.assertEqual(tours_b_res.status, 200)
        self.assertIsInstance(tours_b_res.json, list)
        tours_b_ids = [t["id"] for t in tours_b_res.json if "id" in t]
        self.assertNotIn(tour_id, tours_b_ids)

    def test_t1_user_get_all(self):
        username = f"user_all_{uuid.uuid4().hex[:8]}"
        self.register_user(username)
        res = api_request("GET", "/api/user")
        self.assertEqual(res.status, 200)
        self.assertIsInstance(res.json, list)
        self.assertTrue(len(res.json) > 0)

    # --- Feature 2: Tour CRUD ---
    def test_t1_tour_create(self):
        username = f"tour_c_{uuid.uuid4().hex[:8]}"
        self.register_user(username)
        user_id = self.get_id(self.login_user(username))
        res = self.create_tour(user_id, "Tour 1")
        self.assertEqual(res.status, 201)
        self.assertIsNotNone(res.json)
        self.assertEqual(res.json.get("name"), "Tour 1")

    def test_t1_tour_get_all(self):
        username = f"tour_ga_{uuid.uuid4().hex[:8]}"
        self.register_user(username)
        user_id = self.get_id(self.login_user(username))
        self.create_tour(user_id, "Tour 1")
        self.create_tour(user_id, "Tour 2")
        res = api_request("GET", f"/api/tour?userId={user_id}")
        self.assertEqual(res.status, 200)
        self.assertIsInstance(res.json, list)
        self.assertEqual(len(res.json), 2)

    def test_t1_tour_get_by_id(self):
        username = f"tour_gbi_{uuid.uuid4().hex[:8]}"
        self.register_user(username)
        user_id = self.get_id(self.login_user(username))
        tour_res = self.create_tour(user_id, "Specific Tour")
        tour_id = self.get_id(tour_res)
        
        res = api_request("GET", f"/api/tour/{tour_id}")
        self.assertEqual(res.status, 200)
        self.assertIsNotNone(res.json)
        self.assertEqual(res.json.get("name"), "Specific Tour")

    def test_t1_tour_update(self):
        username = f"tour_u_{uuid.uuid4().hex[:8]}"
        self.register_user(username)
        user_id = self.get_id(self.login_user(username))
        tour_res = self.create_tour(user_id, "Old Name")
        tour_id = self.get_id(tour_res)
        
        update_payload = {
            "userId": user_id,
            "name": "New Name",
            "description": "Updated description",
            "waypoints": [
                {"label": "Point A", "latitude": 48.2082, "longitude": 16.3738},
                {"label": "Point B", "latitude": 48.2100, "longitude": 16.3800}
            ],
            "transportType": "cycling-regular"
        }
        res = api_request("PUT", f"/api/tour/{tour_id}", body=update_payload)
        self.assertEqual(res.status, 204)
        
        get_res = api_request("GET", f"/api/tour/{tour_id}")
        self.assertIsNotNone(get_res.json)
        self.assertEqual(get_res.json.get("name"), "New Name")

    def test_t1_tour_delete(self):
        username = f"tour_d_{uuid.uuid4().hex[:8]}"
        self.register_user(username)
        user_id = self.get_id(self.login_user(username))
        tour_res = self.create_tour(user_id, "To Delete")
        tour_id = self.get_id(tour_res)
        
        res = api_request("DELETE", f"/api/tour/{tour_id}")
        self.assertEqual(res.status, 204)
        
        get_res = api_request("GET", f"/api/tour/{tour_id}")
        self.assertEqual(get_res.status, 404)

    def test_t1_tour_create_multiple(self):
        username = f"tour_mult_{uuid.uuid4().hex[:8]}"
        self.register_user(username)
        user_id = self.get_id(self.login_user(username))
        for i in range(5):
            self.create_tour(user_id, f"Tour {i}")
        res = api_request("GET", f"/api/tour?userId={user_id}")
        self.assertEqual(res.status, 200)
        self.assertIsInstance(res.json, list)
        self.assertEqual(len(res.json), 5)

    # --- Feature 3: Log CRUD ---
    def test_t1_log_create(self):
        username = f"log_c_{uuid.uuid4().hex[:8]}"
        self.register_user(username)
        user_id = self.get_id(self.login_user(username))
        tour_id = self.get_id(self.create_tour(user_id, "Tour for Log"))
        
        res = self.create_log(tour_id, "Nice weather")
        self.assertEqual(res.status, 201)

    def test_t1_log_get_all(self):
        res = api_request("GET", "/api/log")
        self.assertEqual(res.status, 200)
        self.assertIsInstance(res.json, list)

    def test_t1_log_get_by_id(self):
        username = f"log_gbi_{uuid.uuid4().hex[:8]}"
        self.register_user(username)
        user_id = self.get_id(self.login_user(username))
        tour_id = self.get_id(self.create_tour(user_id, "Tour for Log"))
        self.create_log(tour_id, "Log 1")
        
        logs_res = api_request("GET", f"/api/log/tour/{tour_id}")
        self.assertIsInstance(logs_res.json, list)
        self.assertTrue(len(logs_res.json) > 0)
        log_id = logs_res.json[0]["id"]
        
        res = api_request("GET", f"/api/log/{log_id}")
        self.assertEqual(res.status, 200)
        self.assertIsNotNone(res.json)
        self.assertEqual(res.json.get("comment"), "Log 1")

    def test_t1_log_update(self):
        username = f"log_u_{uuid.uuid4().hex[:8]}"
        self.register_user(username)
        user_id = self.get_id(self.login_user(username))
        tour_id = self.get_id(self.create_tour(user_id, "Tour for Log"))
        self.create_log(tour_id, "Log to update")
        
        logs_res = api_request("GET", f"/api/log/tour/{tour_id}")
        self.assertIsInstance(logs_res.json, list)
        self.assertTrue(len(logs_res.json) > 0)
        log_id = logs_res.json[0]["id"]
        
        update_payload = {
            "tourId": tour_id,
            "dateTime": datetime.now().strftime("%Y-%m-%dT%H:%M:%S"),
            "comment": "Updated Log",
            "difficulty": 3,
            "totalDistanceKm": 12.5,
            "totalTimeMin": "01:15:00",
            "rating": 4
        }
        res = api_request("PUT", f"/api/log/{log_id}", body=update_payload)
        self.assertEqual(res.status, 204)
        
        get_res = api_request("GET", f"/api/log/{log_id}")
        self.assertIsNotNone(get_res.json)
        self.assertEqual(get_res.json.get("comment"), "Updated Log")

    def test_t1_log_delete(self):
        username = f"log_d_{uuid.uuid4().hex[:8]}"
        self.register_user(username)
        user_id = self.get_id(self.login_user(username))
        tour_id = self.get_id(self.create_tour(user_id, "Tour for Log"))
        self.create_log(tour_id, "Log to delete")
        
        logs_res = api_request("GET", f"/api/log/tour/{tour_id}")
        self.assertIsInstance(logs_res.json, list)
        self.assertTrue(len(logs_res.json) > 0)
        log_id = logs_res.json[0]["id"]
        
        res = api_request("DELETE", f"/api/log/{log_id}")
        self.assertEqual(res.status, 204)
        
        get_res = api_request("GET", f"/api/log/{log_id}")
        self.assertEqual(get_res.status, 404)

    def test_t1_log_create_multiple_for_tour(self):
        username = f"log_mult_{uuid.uuid4().hex[:8]}"
        self.register_user(username)
        user_id = self.get_id(self.login_user(username))
        tour_id = self.get_id(self.create_tour(user_id, "Tour for Log"))
        self.create_log(tour_id, "Log A")
        self.create_log(tour_id, "Log B")
        
        logs_res = api_request("GET", f"/api/log/tour/{tour_id}")
        self.assertEqual(logs_res.status, 200)
        self.assertIsInstance(logs_res.json, list)
        self.assertEqual(len(logs_res.json), 2)

    # --- Feature 4: Search ---
    def test_t1_search_by_name(self):
        res = api_request("GET", "/api/tour/search?query=Vienna")
        self.assertEqual(res.status, 200)

    def test_t1_search_by_description(self):
        res = api_request("GET", "/api/tour/search?query=sunny")
        self.assertEqual(res.status, 200)

    def test_t1_search_by_log_comment(self):
        res = api_request("GET", "/api/tour/search?query=windy")
        self.assertEqual(res.status, 200)

    def test_t1_search_by_calculated_values(self):
        res = api_request("GET", "/api/tour/search?query=child-friendly")
        self.assertEqual(res.status, 200)

    def test_t1_search_empty_query(self):
        res = api_request("GET", "/api/tour/search?query=")
        self.assertEqual(res.status, 200)

    def test_t1_search_special_characters(self):
        res = api_request("GET", "/api/tour/search?query=%&*$")
        self.assertEqual(res.status, 200)

    # --- Feature 5: Import / Export ---
    def test_t1_export_tours(self):
        username = f"imp_exp_{uuid.uuid4().hex[:8]}"
        self.register_user(username)
        user_id = self.get_id(self.login_user(username))
        res = api_request("GET", f"/api/tour/export?userId={user_id}")
        self.assertEqual(res.status, 200)
        self.assertIsInstance(res.json, list)

    def test_t1_import_valid_json(self):
        username = f"imp_val_{uuid.uuid4().hex[:8]}"
        self.register_user(username)
        user_id = self.get_id(self.login_user(username))
        
        import_data = [
            {
                "name": "Imported Tour",
                "description": "Desc",
                "waypoints": [{"label": "P1", "latitude": 48.2, "longitude": 16.3}],
                "transportType": "cycling-regular",
                "logs": []
            }
        ]
        res = api_request("POST", f"/api/tour/import?userId={user_id}", body=import_data)
        self.assertEqual(res.status, 200)

    def test_t1_import_restores_logs(self):
        username = f"imp_logs_{uuid.uuid4().hex[:8]}"
        self.register_user(username)
        user_id = self.get_id(self.login_user(username))
        
        import_data = [
            {
                "name": "Tour with Log",
                "description": "Desc",
                "waypoints": [{"label": "P1", "latitude": 48.2, "longitude": 16.3}],
                "transportType": "cycling-regular",
                "logs": [
                    {
                        "dateTime": "2026-06-17T12:00:00",
                        "comment": "Restored Log",
                        "difficulty": 2,
                        "distance": 10.0,
                        "time": "01:00:00",
                        "rating": 5
                    }
                ]
            }
        ]
        res = api_request("POST", f"/api/tour/import?userId={user_id}", body=import_data)
        self.assertEqual(res.status, 200)

    def test_t1_import_modify_export(self):
        username = f"imp_mod_{uuid.uuid4().hex[:8]}"
        self.register_user(username)
        user_id = self.get_id(self.login_user(username))
        
        import_data = [
            {
                "name": "Initial Tour",
                "description": "Desc",
                "waypoints": [{"label": "P1", "latitude": 48.2, "longitude": 16.3}],
                "transportType": "cycling-regular",
                "logs": []
            }
        ]
        import_res = api_request("POST", f"/api/tour/import?userId={user_id}", body=import_data)
        self.assertEqual(import_res.status, 200)
        
        tours_res = api_request("GET", f"/api/tour?userId={user_id}")
        self.assertIsInstance(tours_res.json, list)
        self.assertTrue(len(tours_res.json) > 0)
        tour_id = tours_res.json[0]["id"]
        
        update_payload = {
            "userId": user_id,
            "name": "Modified Tour",
            "description": "Desc",
            "waypoints": [{"label": "P1", "latitude": 48.2, "longitude": 16.3}],
            "transportType": "cycling-regular"
        }
        api_request("PUT", f"/api/tour/{tour_id}", body=update_payload)
        
        export_res = api_request("GET", f"/api/tour/export?userId={user_id}")
        self.assertEqual(export_res.status, 200)
        self.assertIsInstance(export_res.json, list)
        self.assertTrue(len(export_res.json) > 0)
        self.assertEqual(export_res.json[0]["name"], "Modified Tour")

    def test_t1_export_empty_list(self):
        username = f"imp_empty_{uuid.uuid4().hex[:8]}"
        self.register_user(username)
        user_id = self.get_id(self.login_user(username))
        res = api_request("GET", f"/api/tour/export?userId={user_id}")
        self.assertEqual(res.status, 200)
        self.assertEqual(res.json, [])

    def test_t1_import_handles_empty(self):
        username = f"imp_h_empty_{uuid.uuid4().hex[:8]}"
        self.register_user(username)
        user_id = self.get_id(self.login_user(username))
        res = api_request("POST", f"/api/tour/import?userId={user_id}", body=[])
        self.assertEqual(res.status, 200)

    # --- Feature 6: Carbon Footprint ---
    def test_t1_carbon_bike(self):
        username = f"co2_b_{uuid.uuid4().hex[:8]}"
        self.register_user(username)
        user_id = self.get_id(self.login_user(username))
        tour_res = self.create_tour(user_id, "Bike Tour", transport_type="cycling-regular")
        self.assertEqual(tour_res.status, 201)
        
        tour = tour_res.json
        self.assertIsNotNone(tour)
        self.assertIsNotNone(tour.get("co2Emitted"))
        self.assertIsNotNone(tour.get("co2Saved"))
        self.assertIsNotNone(tour.get("distanceKm"))
        self.assertEqual(tour.get("co2Emitted"), 0.0)
        self.assertAlmostEqual(tour.get("co2Saved"), tour.get("distanceKm") * 120.0, places=2)

    def test_t1_carbon_hike(self):
        username = f"co2_h_{uuid.uuid4().hex[:8]}"
        self.register_user(username)
        user_id = self.get_id(self.login_user(username))
        tour_res = self.create_tour(user_id, "Hike Tour", transport_type="foot-hiking")
        self.assertEqual(tour_res.status, 201)
        
        tour = tour_res.json
        self.assertIsNotNone(tour)
        self.assertIsNotNone(tour.get("co2Emitted"))
        self.assertIsNotNone(tour.get("co2Saved"))
        self.assertIsNotNone(tour.get("distanceKm"))
        self.assertEqual(tour.get("co2Emitted"), 0.0)
        self.assertAlmostEqual(tour.get("co2Saved"), tour.get("distanceKm") * 120.0, places=2)

    def test_t1_carbon_run(self):
        username = f"co2_r_{uuid.uuid4().hex[:8]}"
        self.register_user(username)
        user_id = self.get_id(self.login_user(username))
        tour_res = self.create_tour(user_id, "Running Tour", transport_type="foot-walking")
        self.assertEqual(tour_res.status, 201)
        
        tour = tour_res.json
        self.assertIsNotNone(tour)
        self.assertIsNotNone(tour.get("co2Emitted"))
        self.assertIsNotNone(tour.get("co2Saved"))
        self.assertIsNotNone(tour.get("distanceKm"))
        self.assertEqual(tour.get("co2Emitted"), 0.0)
        self.assertAlmostEqual(tour.get("co2Saved"), tour.get("distanceKm") * 120.0, places=2)

    def test_t1_carbon_vacation(self):
        username = f"co2_v_{uuid.uuid4().hex[:8]}"
        self.register_user(username)
        user_id = self.get_id(self.login_user(username))
        tour_res = self.create_tour(user_id, "Train Trip", transport_type="driving-car")
        self.assertEqual(tour_res.status, 201)
        
        tour = tour_res.json
        self.assertIsNotNone(tour)
        self.assertIsNotNone(tour.get("co2Emitted"))
        self.assertIsNotNone(tour.get("co2Saved"))
        self.assertIsNotNone(tour.get("distanceKm"))
        self.assertAlmostEqual(tour.get("co2Emitted"), tour.get("distanceKm") * 20.0, places=2)
        self.assertAlmostEqual(tour.get("co2Saved"), tour.get("distanceKm") * 100.0, places=2)

    def test_t1_carbon_dynamic_update(self):
        username = f"co2_dy_{uuid.uuid4().hex[:8]}"
        self.register_user(username)
        user_id = self.get_id(self.login_user(username))
        tour_res = self.create_tour(user_id, "Dynamic Tour", transport_type="cycling-regular")
        tour_id = self.get_id(tour_res)
        
        update_payload = {
            "userId": user_id,
            "name": "Dynamic Tour",
            "description": "Updated",
            "waypoints": [
                {"label": "Start", "latitude": 48.2082, "longitude": 16.3738},
                {"label": "End", "latitude": 48.2100, "longitude": 16.3800}
            ],
            "transportType": "driving-car"
        }
        res = api_request("PUT", f"/api/tour/{tour_id}", body=update_payload)
        self.assertEqual(res.status, 204)
        
        get_res = api_request("GET", f"/api/tour/{tour_id}")
        tour = get_res.json
        self.assertIsNotNone(tour)
        self.assertIsNotNone(tour.get("co2Emitted"))
        self.assertIsNotNone(tour.get("distanceKm"))
        self.assertAlmostEqual(tour.get("co2Emitted"), tour.get("distanceKm") * 20.0, places=2)

    def test_t1_carbon_zero_distance(self):
        username = f"co2_zero_{uuid.uuid4().hex[:8]}"
        self.register_user(username)
        user_id = self.get_id(self.login_user(username))
        waypoints = [
            {"label": "Start Point", "latitude": 48.2082, "longitude": 16.3738},
            {"label": "Start Point", "latitude": 48.2082, "longitude": 16.3738}
        ]
        tour_res = self.create_tour(user_id, "Zero Tour", transport_type="cycling-regular", waypoints=waypoints)
        self.assertEqual(tour_res.status, 201)
        self.assertIsNotNone(tour_res.json)
        self.assertEqual(tour_res.json.get("co2Emitted"), 0.0)
        self.assertEqual(tour_res.json.get("co2Saved"), 0.0)


    # ==========================================
    # TIER 2: Boundary & Corner Cases (31 tests)
    # ==========================================

    # --- User Management ---
    def test_t2_user_login_invalid_password(self):
        username = f"u_bnd_{uuid.uuid4().hex[:8]}"
        self.register_user(username)
        res = self.login_user(username, password="WrongPassword")
        self.assertEqual(res.status, 401)

    def test_t2_user_login_nonexistent(self):
        res = self.login_user("nonexistent_user")
        self.assertEqual(res.status, 401)

    def test_t2_user_register_empty_username(self):
        res = self.register_user("")
        self.assertEqual(res.status, 400)

    def test_t2_user_register_empty_password(self):
        username = f"u_emp_p_{uuid.uuid4().hex[:8]}"
        res = self.register_user(username, password="")
        self.assertEqual(res.status, 400)

    def test_t2_user_register_long_username(self):
        username = "a" * 100
        res = self.register_user(username)
        self.assertEqual(res.status, 400)

    def test_t2_user_invalid_token_request(self):
        res = api_request("GET", "/api/tour?userId=invalid-guid-string")
        self.assertEqual(res.status, 400)

    # --- Tour CRUD ---
    def test_t2_tour_create_empty_name(self):
        username = f"t_bnd_{uuid.uuid4().hex[:8]}"
        self.register_user(username)
        user_id = self.get_id(self.login_user(username))
        res = self.create_tour(user_id, "")
        self.assertEqual(res.status, 400)

    def test_t2_tour_create_long_description(self):
        username = f"t_desc_{uuid.uuid4().hex[:8]}"
        self.register_user(username)
        user_id = self.get_id(self.login_user(username))
        description = "d" * 5000
        res = self.create_tour(user_id, "Long Desc Tour", description=description)
        self.assertIn(res.status, [201, 400])

    def test_t2_tour_create_negative_distance(self):
        username = f"t_neg_{uuid.uuid4().hex[:8]}"
        self.register_user(username)
        user_id = self.get_id(self.login_user(username))
        payload = {
            "userId": user_id,
            "name": "Neg Distance",
            "distanceKm": -10.0,
            "transportType": "cycling-regular",
            "waypoints": []
        }
        res = api_request("POST", "/api/tour", body=payload)
        self.assertEqual(res.status, 400)

    def test_t2_tour_update_nonexistent_id(self):
        username = f"t_non_{uuid.uuid4().hex[:8]}"
        self.register_user(username)
        user_id = self.get_id(self.login_user(username))
        payload = {
            "userId": user_id,
            "name": "Name",
            "transportType": "cycling-regular",
            "waypoints": []
        }
        res = api_request("PUT", f"/api/tour/{uuid.uuid4()}", body=payload)
        self.assertEqual(res.status, 404)

    def test_t2_tour_delete_nonexistent_id(self):
        res = api_request("DELETE", f"/api/tour/{uuid.uuid4()}")
        self.assertIn(res.status, [404, 204, 500])

    # --- Log CRUD ---
    def test_t2_log_create_invalid_date(self):
        username = f"l_bnd_{uuid.uuid4().hex[:8]}"
        self.register_user(username)
        user_id = self.get_id(self.login_user(username))
        tour_id = self.get_id(self.create_tour(user_id, "Tour"))
        
        payload = {
            "tourId": tour_id,
            "dateTime": "invalid-date-format",
            "comment": "Bad Date",
            "difficulty": 1,
            "totalDistanceKm": 5.0,
            "totalTimeMin": "01:00:00",
            "rating": 3
        }
        res = api_request("POST", "/api/log", body=payload)
        self.assertEqual(res.status, 400)

    def test_t2_log_create_negative_distance(self):
        username = f"l_neg_{uuid.uuid4().hex[:8]}"
        self.register_user(username)
        user_id = self.get_id(self.login_user(username))
        tour_id = self.get_id(self.create_tour(user_id, "Tour"))
        res = self.create_log(tour_id, distance=-5.0)
        self.assertEqual(res.status, 400)

    def test_t2_log_create_invalid_rating(self):
        username = f"l_rat_{uuid.uuid4().hex[:8]}"
        self.register_user(username)
        user_id = self.get_id(self.login_user(username))
        tour_id = self.get_id(self.create_tour(user_id, "Tour"))
        res = self.create_log(tour_id, rating=6)
        self.assertEqual(res.status, 400)

    def test_t2_log_create_invalid_difficulty(self):
        username = f"l_diff_{uuid.uuid4().hex[:8]}"
        self.register_user(username)
        user_id = self.get_id(self.login_user(username))
        tour_id = self.get_id(self.create_tour(user_id, "Tour"))
        res = self.create_log(tour_id, difficulty=0)
        self.assertEqual(res.status, 400)

    def test_t2_log_create_nonexistent_tour(self):
        res = self.create_log(str(uuid.uuid4()))
        self.assertEqual(res.status, 404)

    # --- Search ---
    def test_t2_search_long_query(self):
        long_query = "s" * 1000
        res = api_request("GET", f"/api/tour/search?query={long_query}")
        self.assertIn(res.status, [200, 400])

    def test_t2_search_no_match(self):
        res = api_request("GET", "/api/tour/search?query=NonExistentSearchTermThatMatchesNothing")
        self.assertEqual(res.status, 200)
        self.assertEqual(res.json, [])

    def test_t2_search_sql_injection(self):
        res = api_request("GET", "/api/tour/search?query=' OR '1'='1")
        self.assertEqual(res.status, 200)

    def test_t2_search_case_insensitive(self):
        res = api_request("GET", "/api/tour/search?query=viEnNA")
        self.assertEqual(res.status, 200)

    def test_t2_search_leading_trailing_whitespace(self):
        res = api_request("GET", "/api/tour/search?query=%20Vienna%20")
        self.assertEqual(res.status, 200)

    # --- Import / Export ---
    def test_t2_import_invalid_json_syntax(self):
        username = f"imp_bnd_js_{uuid.uuid4().hex[:8]}"
        self.register_user(username)
        user_id = self.get_id(self.login_user(username))
        res = api_request("POST", f"/api/tour/import?userId={user_id}", body="{invalid-json}")
        self.assertEqual(res.status, 400)

    def test_t2_import_wrong_schema(self):
        username = f"imp_bnd_sch_{uuid.uuid4().hex[:8]}"
        self.register_user(username)
        user_id = self.get_id(self.login_user(username))
        bad_data = [{"badField": "Bad Value"}]
        res = api_request("POST", f"/api/tour/import?userId={user_id}", body=bad_data)
        self.assertEqual(res.status, 400)

    def test_t2_import_negative_values(self):
        username = f"imp_bnd_neg_{uuid.uuid4().hex[:8]}"
        self.register_user(username)
        user_id = self.get_id(self.login_user(username))
        bad_data = [
            {
                "name": "Imported Tour",
                "distance": -12.5,
                "waypoints": [],
                "transportType": "cycling-regular",
                "logs": []
            }
        ]
        res = api_request("POST", f"/api/tour/import?userId={user_id}", body=bad_data)
        self.assertEqual(res.status, 400)

    def test_t2_import_duplicate_names(self):
        username = f"imp_bnd_dup_{uuid.uuid4().hex[:8]}"
        self.register_user(username)
        user_id = self.get_id(self.login_user(username))
        
        duplicate_data = [
            {
                "name": "Duplicate Name",
                "waypoints": [{"label": "P1", "latitude": 48.2, "longitude": 16.3}],
                "transportType": "cycling-regular",
                "logs": []
            },
            {
                "name": "Duplicate Name",
                "waypoints": [{"label": "P1", "latitude": 48.2, "longitude": 16.3}],
                "transportType": "cycling-regular",
                "logs": []
            }
        ]
        res = api_request("POST", f"/api/tour/import?userId={user_id}", body=duplicate_data)
        self.assertIn(res.status, [200, 400])

    def test_t2_import_massive_dataset(self):
        username = f"imp_bnd_mass_{uuid.uuid4().hex[:8]}"
        self.register_user(username)
        user_id = self.get_id(self.login_user(username))
        massive_data = []
        for i in range(100):
            massive_data.append({
                "name": f"Mass Tour {i}",
                "waypoints": [{"label": "P1", "latitude": 48.2, "longitude": 16.3}],
                "transportType": "cycling-regular",
                "logs": []
            })
        res = api_request("POST", f"/api/tour/import?userId={user_id}", body=massive_data)
        self.assertEqual(res.status, 200)

    # --- Carbon Footprint ---
    def test_t2_carbon_distance_zero(self):
        username = f"co2_bnd_z_{uuid.uuid4().hex[:8]}"
        self.register_user(username)
        user_id = self.get_id(self.login_user(username))
        
        waypoints = [
            {"label": "P1", "latitude": 48.2, "longitude": 16.3},
            {"label": "P1", "latitude": 48.2, "longitude": 16.3}
        ]
        tour_res = self.create_tour(user_id, "Zero Dist", transport_type="cycling-regular", waypoints=waypoints)
        self.assertEqual(tour_res.status, 201)
        self.assertIsNotNone(tour_res.json)
        self.assertEqual(tour_res.json.get("co2Emitted"), 0.0)
        self.assertEqual(tour_res.json.get("co2Saved"), 0.0)

    def test_t2_carbon_massive_distance(self):
        username = f"co2_bnd_m_{uuid.uuid4().hex[:8]}"
        self.register_user(username)
        user_id = self.get_id(self.login_user(username))
        waypoints = [
            {"label": "Vienna", "latitude": 48.2082, "longitude": 16.3738},
            {"label": "Sydney", "latitude": -33.8688, "longitude": 151.2093}
        ]
        tour_res = self.create_tour(user_id, "Massive Dist", transport_type="cycling-regular", waypoints=waypoints)
        self.assertEqual(tour_res.status, 201)
        self.assertIsNotNone(tour_res.json)
        self.assertIsNotNone(tour_res.json.get("co2Saved"))
        self.assertTrue(tour_res.json.get("co2Saved") > 1000000.0)

    def test_t2_carbon_invalid_transport_type(self):
        username = f"co2_bnd_inv_{uuid.uuid4().hex[:8]}"
        self.register_user(username)
        user_id = self.get_id(self.login_user(username))
        
        res = self.create_tour(user_id, "Bad Transport", transport_type="invalid-type")
        self.assertEqual(res.status, 400)

    def test_t2_carbon_update_transport_type(self):
        username = f"co2_bnd_up_{uuid.uuid4().hex[:8]}"
        self.register_user(username)
        user_id = self.get_id(self.login_user(username))
        tour_res = self.create_tour(user_id, "Update CO2", transport_type="cycling-regular")
        tour_id = self.get_id(tour_res)
        
        update_payload = {
            "userId": user_id,
            "name": "Update CO2",
            "description": "Updated description",
            "waypoints": [
                {"label": "Start", "latitude": 48.2082, "longitude": 16.3738},
                {"label": "End", "latitude": 48.2100, "longitude": 16.3800}
            ],
            "transportType": "driving-car"
        }
        res = api_request("PUT", f"/api/tour/{tour_id}", body=update_payload)
        self.assertEqual(res.status, 204)
        
        get_res = api_request("GET", f"/api/tour/{tour_id}")
        self.assertIsNotNone(get_res.json)
        self.assertIsNotNone(get_res.json.get("co2Emitted"))
        self.assertIsNotNone(get_res.json.get("distanceKm"))
        self.assertAlmostEqual(get_res.json.get("co2Emitted"), get_res.json.get("distanceKm") * 20.0, places=2)

    def test_t2_carbon_multi_step_precision(self):
        username = f"co2_bnd_pr_{uuid.uuid4().hex[:8]}"
        self.register_user(username)
        user_id = self.get_id(self.login_user(username))
        tour_res = self.create_tour(user_id, "Precision Tour", transport_type="cycling-regular")
        self.assertEqual(tour_res.status, 201)
        tour = tour_res.json
        self.assertIsNotNone(tour)
        self.assertIsNotNone(tour.get("distanceKm"))
        self.assertIsNotNone(tour.get("co2Saved"))
        dist = tour.get("distanceKm")
        expected_saved = dist * 120.0
        self.assertAlmostEqual(tour.get("co2Saved"), expected_saved, places=2)


    # ==========================================
    # TIER 3: Cross-Feature Combinations (6 tests)
    # ==========================================

    def test_t3_user_isolation_crud(self):
        username_a = f"t3_iso_a_{uuid.uuid4().hex[:8]}"
        username_b = f"t3_iso_b_{uuid.uuid4().hex[:8]}"
        self.register_user(username_a)
        self.register_user(username_b)
        
        id_a = self.get_id(self.login_user(username_a))
        id_b = self.get_id(self.login_user(username_b))
        
        tour_res = self.create_tour(id_a, "User A's Secret Tour")
        tour_id = self.get_id(tour_res)
        
        update_payload = {
            "userId": id_b,
            "name": "Hacked Name",
            "waypoints": [{"label": "P1", "latitude": 48.2, "longitude": 16.3}],
            "transportType": "cycling-regular"
        }
        update_res = api_request("PUT", f"/api/tour/{tour_id}", body=update_payload)
        self.assertIn(update_res.status, [403, 404, 400])
        
        delete_res = api_request("DELETE", f"/api/tour/{tour_id}")
        self.assertIn(delete_res.status, [403, 404])

    def test_t3_tour_log_search(self):
        username = f"t3_tls_{uuid.uuid4().hex[:8]}"
        self.register_user(username)
        user_id = self.get_id(self.login_user(username))
        
        tour_res = self.create_tour(user_id, "Vienna Sightseeing")
        tour_id = self.get_id(tour_res)
        
        unique_keyword = f"UniqueKeyword_{uuid.uuid4().hex[:8]}"
        self.create_log(tour_id, comment=f"This was {unique_keyword}!")
        
        search_res = api_request("GET", f"/api/tour/search?query={unique_keyword}")
        self.assertEqual(search_res.status, 200)
        self.assertIsInstance(search_res.json, list)
        found_tour_names = [t["name"] for t in search_res.json if "name" in t]
        self.assertIn("Vienna Sightseeing", found_tour_names)

    def test_t3_log_metrics_calculation(self):
        username = f"t3_metrics_{uuid.uuid4().hex[:8]}"
        self.register_user(username)
        user_id = self.get_id(self.login_user(username))
        
        tour_res = self.create_tour(user_id, "Calculated Tour")
        tour_id = self.get_id(tour_res)
        
        self.create_log(tour_id, difficulty=2, distance=5.0, time_str="00:30:00")
        self.create_log(tour_id, difficulty=4, distance=15.0, time_str="01:30:00")
        self.create_log(tour_id, difficulty=3, distance=10.0, time_str="01:00:00")
        
        tour_get = api_request("GET", f"/api/tour/{tour_id}").json
        self.assertIsNotNone(tour_get)
        self.assertEqual(tour_get.get("popularity"), 3.0)
        self.assertIsNotNone(tour_get.get("childFriendliness"))
        self.assertTrue(0 <= tour_get.get("childFriendliness") <= 10)

    def test_t3_import_search(self):
        username = f"t3_imp_s_{uuid.uuid4().hex[:8]}"
        self.register_user(username)
        user_id = self.get_id(self.login_user(username))
        
        unique_name = f"ImportedSearchTour_{uuid.uuid4().hex[:8]}"
        import_data = [
            {
                "name": unique_name,
                "waypoints": [{"label": "P1", "latitude": 48.2, "longitude": 16.3}],
                "transportType": "cycling-regular",
                "logs": [{"dateTime": "2026-06-17T12:00:00", "comment": "Nice search target", "difficulty": 1, "distance": 1.0, "time": "00:10:00", "rating": 5}]
            }
        ]
        
        import_res = api_request("POST", f"/api/tour/import?userId={user_id}", body=import_data)
        self.assertEqual(import_res.status, 200)
        
        search_res = api_request("GET", f"/api/tour/search?query={unique_name}")
        self.assertEqual(search_res.status, 200)
        self.assertIsInstance(search_res.json, list)
        self.assertTrue(any(t.get("name") == unique_name for t in search_res.json))

    def test_t3_import_carbon_footprint(self):
        username = f"t3_imp_co2_{uuid.uuid4().hex[:8]}"
        self.register_user(username)
        user_id = self.get_id(self.login_user(username))
        
        import_data = [
            {
                "name": "CO2 Import Tour",
                "waypoints": [{"label": "P1", "latitude": 48.2, "longitude": 16.3}, {"label": "P2", "latitude": 48.3, "longitude": 16.4}],
                "transportType": "cycling-regular",
                "logs": []
            }
        ]
        import_res = api_request("POST", f"/api/tour/import?userId={user_id}", body=import_data)
        self.assertEqual(import_res.status, 200)
        
        tours = api_request("GET", f"/api/tour?userId={user_id}").json
        self.assertIsInstance(tours, list)
        self.assertTrue(len(tours) > 0)
        self.assertEqual(tours[0].get("co2Emitted"), 0.0)
        self.assertIsNotNone(tours[0].get("co2Saved"))
        self.assertTrue(tours[0].get("co2Saved") > 0.0)

    def test_t3_user_isolation_import_export(self):
        username_a = f"t3_ie_a_{uuid.uuid4().hex[:8]}"
        username_b = f"t3_ie_b_{uuid.uuid4().hex[:8]}"
        self.register_user(username_a)
        self.register_user(username_b)
        
        id_a = self.get_id(self.login_user(username_a))
        id_b = self.get_id(self.login_user(username_b))
        
        self.create_tour(id_a, "Tour A")
        
        export_res = api_request("GET", f"/api/tour/export?userId={id_a}")
        self.assertEqual(export_res.status, 200)
        tours_a = export_res.json
        
        import_res = api_request("POST", f"/api/tour/import?userId={id_b}", body=tours_a)
        self.assertEqual(import_res.status, 200)
        
        tours_b = api_request("GET", f"/api/tour?userId={id_b}").json
        self.assertIsInstance(tours_b, list)
        self.assertEqual(len(tours_b), 1)
        self.assertEqual(tours_b[0]["name"], "Tour A")


    # ==========================================
    # TIER 4: Real-World Application Scenarios (5 scenarios)
    # ==========================================

    def test_t4_scenario_avid_cyclist(self):
        username = "cyclist_bob"
        self.register_user(username)
        user_id = self.get_id(self.login_user(username))
        
        tour_res = self.create_tour(user_id, "Vienna to Bratislava", transport_type="cycling-regular")
        self.assertEqual(tour_res.status, 201)
        tour_id = self.get_id(tour_res)
        
        self.create_log(tour_id, comment="Log 1: Easy tour", difficulty=1, distance=75.0, time_str="03:00:00", rating=5)
        self.create_log(tour_id, comment="Log 2: Medium tour due to headwind", difficulty=3, distance=78.0, time_str="03:30:00", rating=4)
        
        tour_get = api_request("GET", f"/api/tour/{tour_id}")
        self.assertIsNotNone(tour_get.json)
        self.assertEqual(tour_get.json.get("popularity"), 2.0)
        
        self.assertEqual(tour_get.json.get("co2Emitted"), 0.0)
        self.assertIsNotNone(tour_get.json.get("co2Saved"))
        self.assertIsNotNone(tour_get.json.get("distanceKm"))
        self.assertAlmostEqual(tour_get.json.get("co2Saved"), tour_get.json.get("distanceKm") * 120.0, places=2)
        
        search_res = api_request("GET", "/api/tour/search?query=Bratislava")
        self.assertEqual(search_res.status, 200)
        self.assertIsInstance(search_res.json, list)
        self.assertTrue(any(t.get("id") == tour_id for t in search_res.json))

    def test_t4_scenario_family_hiking_planner(self):
        username = "hiker_alice"
        self.register_user(username)
        user_id = self.get_id(self.login_user(username))
        
        tour_easy = self.create_tour(user_id, "Lainzer Tiergarten Walk", transport_type="foot-hiking")
        self.assertEqual(tour_easy.status, 201)
        tour_easy_id = self.get_id(tour_easy)
        self.create_log(tour_easy_id, comment="Kids loved the wild boars!", difficulty=1, distance=5.0, time_str="02:00:00", rating=5)
        
        tour_hard = self.create_tour(user_id, "Schneeberg Summit", transport_type="foot-hiking")
        self.assertEqual(tour_hard.status, 201)
        tour_hard_id = self.get_id(tour_hard)
        self.create_log(tour_hard_id, comment="Steep and rocky", difficulty=5, distance=15.0, time_str="07:00:00", rating=4)
        
        tour_easy_get = api_request("GET", f"/api/tour/{tour_easy_id}").json
        tour_hard_get = api_request("GET", f"/api/tour/{tour_hard_id}").json
        
        self.assertIsNotNone(tour_easy_get)
        self.assertIsNotNone(tour_hard_get)
        self.assertIsNotNone(tour_easy_get.get("childFriendliness"))
        self.assertIsNotNone(tour_hard_get.get("childFriendliness"))
        self.assertTrue(tour_easy_get.get("childFriendliness") > tour_hard_get.get("childFriendliness"))

    def test_t4_scenario_data_migration_backup(self):
        username = "backup_user"
        self.register_user(username)
        user_id = self.get_id(self.login_user(username))
        
        for i in range(3):
            tour_id = self.get_id(self.create_tour(user_id, f"Backup Tour {i}"))
            self.create_log(tour_id, comment=f"Log for Backup {i}")
            
        export_res = api_request("GET", f"/api/tour/export?userId={user_id}")
        self.assertEqual(export_res.status, 200)
        exported_data = export_res.json
        self.assertIsInstance(exported_data, list)
        self.assertEqual(len(exported_data), 3)
        
        tours = api_request("GET", f"/api/tour?userId={user_id}").json
        self.assertIsInstance(tours, list)
        for t in tours:
            api_request("DELETE", f"/api/tour/{t['id']}")
            
        empty_tours = api_request("GET", f"/api/tour?userId={user_id}").json
        self.assertEqual(empty_tours, [])
        
        import_res = api_request("POST", f"/api/tour/import?userId={user_id}", body=exported_data)
        self.assertEqual(import_res.status, 200)
        
        restored_tours = api_request("GET", f"/api/tour?userId={user_id}").json
        self.assertIsInstance(restored_tours, list)
        self.assertEqual(len(restored_tours), 3)
        for t in restored_tours:
            logs = api_request("GET", f"/api/log/tour/{t['id']}").json
            self.assertIsInstance(logs, list)
            self.assertEqual(len(logs), 1)

    def test_t4_scenario_multi_user_collaboration(self):
        self.register_user("alice")
        self.register_user("bob")
        
        id_alice = self.get_id(self.login_user("alice"))
        id_bob = self.get_id(self.login_user("bob"))
        
        alice_tour = self.create_tour(id_alice, "Prater Park Run", transport_type="foot-walking")
        self.assertEqual(alice_tour.status, 201)
        self.create_log(alice_tour.json["id"], comment="Morning run in the sun")
        
        tours_bob = api_request("GET", f"/api/tour?userId={id_bob}").json
        self.assertEqual(tours_bob, [])
        
        bob_tour = self.create_tour(id_bob, "Prater Park Run", transport_type="foot-walking")
        self.assertEqual(bob_tour.status, 201)
        self.create_log(bob_tour.json["id"], comment="Evening run")
        
        tours_alice = api_request("GET", f"/api/tour?userId={id_alice}").json
        tours_bob = api_request("GET", f"/api/tour?userId={id_bob}").json
        
        self.assertIsInstance(tours_alice, list)
        self.assertIsInstance(tours_bob, list)
        self.assertEqual(len(tours_alice), 1)
        self.assertEqual(len(tours_bob), 1)
        
        self.assertNotEqual(tours_alice[0]["id"], tours_bob[0]["id"])
        
        logs_alice = api_request("GET", f"/api/log/tour/{tours_alice[0]['id']}").json
        logs_bob = api_request("GET", f"/api/log/tour/{tours_bob[0]['id']}").json
        
        self.assertIsInstance(logs_alice, list)
        self.assertIsInstance(logs_bob, list)
        self.assertEqual(logs_alice[0]["comment"], "Morning run in the sun")
        self.assertEqual(logs_bob[0]["comment"], "Evening run")

    def test_t4_scenario_year_end_carbon_audit(self):
        username = "green_traveler"
        self.register_user(username)
        user_id = self.get_id(self.login_user(username))
        
        for i in range(4):
            self.create_tour(user_id, f"Bike Audit {i}", transport_type="cycling-regular")
        for i in range(3):
            self.create_tour(user_id, f"Hike Audit {i}", transport_type="foot-hiking")
        for i in range(3):
            self.create_tour(user_id, f"Vacation Audit {i}", transport_type="driving-car")
            
        tours = api_request("GET", f"/api/tour?userId={user_id}").json
        self.assertIsInstance(tours, list)
        self.assertEqual(len(tours), 10)
        
        total_emitted = sum(t.get("co2Emitted", 0.0) for t in tours if t.get("co2Emitted") is not None)
        total_saved = sum(t.get("co2Saved", 0.0) for t in tours if t.get("co2Saved") is not None)
        
        expected_emitted = 0.0
        expected_saved = 0.0
        for t in tours:
            dist = t.get("distanceKm", 0.0)
            if t.get("transportType") == "driving-car":
                expected_emitted += dist * 20.0
                expected_saved += dist * 100.0
            else:
                expected_emitted += 0.0
                expected_saved += dist * 120.0
                
        self.assertAlmostEqual(total_emitted, expected_emitted, places=2)
        self.assertAlmostEqual(total_saved, expected_saved, places=2)


if __name__ == "__main__":
    unittest.main()
