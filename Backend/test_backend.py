"""
Automated validation test suite for Semiconductor Digital Twin Backend.
Tests all Module 9 microservices endpoints using FastAPI TestClient.
"""

import sys
import os

# Ensure local backend directory is in path
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

from fastapi.testclient import TestClient
from main import app

client = TestClient(app)

def test_health_check():
    response = client.get("/health")
    assert response.status_code == 200
    data = response.json()
    assert data["status"] == "healthy"
    print(" [PASS] Health check endpoint")

def test_session_lifecycle():
    # 1. Create Session
    create_res = client.post("/api/v1/sessions/", json={
        "student_id": "STUDENT_23MIC0078",
        "module_name": "Photolithography_Etching"
    })
    assert create_res.status_code == 200
    s_data = create_res.json()
    session_id = s_data["session_id"]
    assert s_data["status"] == "ACTIVE"
    print(f" [PASS] Session creation: {session_id}")

    # 2. Complete Session
    comp_res = client.post(f"/api/v1/sessions/{session_id}/complete", json={
        "yield_percentage": 94.5
    })
    assert comp_res.status_code == 200
    assert comp_res.json()["status"] == "COMPLETED"
    assert comp_res.json()["final_yield"] == 94.5
    print(" [PASS] Session completion & yield tracking")
    return session_id

def test_wafer_serialization(session_id):
    import uuid
    test_waf_id = f"WAF-TEST-{uuid.uuid4().hex[:6]}"

    # Post initial state
    res1 = client.post("/api/v1/wafers/", json={
        "session_id": session_id,
        "wafer_id": test_waf_id,
        "state": "BareSubstrate",
        "revision": 0,
        "diameter": 100.0,
        "thickness": 0.5,
        "substrate_material": "Silicon",
        "crystal_orientation": "<100>",
        "surface_state": "Clean",
        "defect_count": 0
    })
    assert res1.status_code == 200

    # Post revision 1 (after spin coat)
    res2 = client.post("/api/v1/wafers/", json={
        "session_id": session_id,
        "wafer_id": test_waf_id,
        "state": "ResistCoated",
        "revision": 1,
        "diameter": 100.0,
        "thickness": 0.5012,
        "substrate_material": "Silicon",
        "crystal_orientation": "<100>",
        "surface_state": "ResistAZ1512",
        "defect_count": 0
    })
    assert res2.status_code == 200

    # Query revision history for time-travel verification
    hist_res = client.get(f"/api/v1/wafers/{test_waf_id}/history")
    assert hist_res.status_code == 200
    history = hist_res.json()
    assert len(history) == 2
    assert history[0]["revision"] == 0
    assert history[1]["revision"] == 1
    print(" [PASS] Wafer state serialization & revision history")

def test_equipment_telemetry(session_id):
    # Log sensor readings
    t1 = client.post("/api/v1/telemetry/", json={
        "session_id": session_id,
        "equipment_name": "PlasmaEtcher",
        "parameter_name": "RF_Power",
        "parameter_value": 305.2,
        "unit": "W"
    })
    assert t1.status_code == 200

    t2 = client.post("/api/v1/telemetry/", json={
        "session_id": session_id,
        "equipment_name": "PlasmaEtcher",
        "parameter_name": "Chamber_Pressure",
        "parameter_value": 20.4,
        "unit": "mTorr"
    })
    assert t2.status_code == 200

    latest_res = client.get("/api/v1/telemetry/latest?equipment=PlasmaEtcher")
    assert latest_res.status_code == 200
    records = latest_res.json()
    assert len(records) >= 2
    print(" [PASS] Equipment telemetry ingestion & queries")

def test_defect_logging_and_yield_summary(session_id):
    # Log defects
    d1 = client.post("/api/v1/defects/", json={
        "session_id": session_id,
        "wafer_id": "WAF-TEST-42",
        "defect_id": "DEF-101",
        "defect_type": "ResistStriation",
        "severity": 0.35,
        "is_critical": False,
        "origin_equipment": "SpinCoater",
        "pos_x": 12.5,
        "pos_y": -8.2,
        "pos_z": 0.0012
    })
    assert d1.status_code == 200

    d2 = client.post("/api/v1/defects/", json={
        "session_id": session_id,
        "wafer_id": "WAF-TEST-42",
        "defect_id": "DEF-102",
        "defect_type": "SeverePinholes",
        "severity": 0.85,
        "is_critical": True,
        "origin_equipment": "Hotplate",
        "pos_x": -5.0,
        "pos_y": 14.1,
        "pos_z": 0.0012
    })
    assert d2.status_code == 200

    summary_res = client.get(f"/api/v1/defects/summary?session_id={session_id}")
    assert summary_res.status_code == 200
    summary = summary_res.json()
    assert summary["total_defects"] == 2
    assert summary["critical_defects"] == 1
    assert "ResistStriation" in summary["defects_by_type"]
    print(" [PASS] Defect logging & Murphy yield estimation")

def test_recipes():
    recipe_res = client.get("/api/v1/recipes/PlasmaEtcher")
    assert recipe_res.status_code == 200
    data = recipe_res.json()
    assert data["parameters"]["rf_bias_power_w"] == 300.0
    print(" [PASS] Baseline fabrication recipe retrieval")

def test_socratic_bkt():
    # Student answers correctly
    soc_res = client.post("/api/v1/socratic/assess", json={
        "student_id": "STUDENT_TEST_01",
        "concept_id": "SpinDynamics",
        "response_correct": True
    })
    assert soc_res.status_code == 200
    data = soc_res.json()
    assert data["posterior_mastery"] > data["prior_mastery"]

    # Check mastery profile
    prof_res = client.get("/api/v1/socratic/mastery/STUDENT_TEST_01")
    assert prof_res.status_code == 200
    prof = prof_res.json()
    assert "SpinDynamics" in prof["concepts"]
    print(f" [PASS] Bayesian Knowledge Tracing (BKT Eq. 8): mastery updated to {data['posterior_mastery']}")

if __name__ == "__main__":
    print("================================================================")
    print("Testing Semiconductor Digital Twin Backend (Module 9)...")
    print("================================================================")
    test_health_check()
    sess_id = test_session_lifecycle()
    test_wafer_serialization(sess_id)
    test_equipment_telemetry(sess_id)
    test_defect_logging_and_yield_summary(sess_id)
    test_recipes()
    test_socratic_bkt()
    print("================================================================")
    print("ALL TEST SUITES PASSED CLEANLY (100% SUCCESS)!")
    print("================================================================")
