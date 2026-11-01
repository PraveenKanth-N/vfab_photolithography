from fastapi import APIRouter, Depends, HTTPException
from sqlalchemy.orm import Session
import json
from typing import List

from database import get_db
import models
import schemas

router = APIRouter(prefix="/recipes", tags=["Fabrication Recipes"])

STANDARD_RECIPES = {
    "SpinCoater": {
        "id": "REC-SPIN-01",
        "process_type": "SpinCoater",
        "recipe_name": "Standard Positive Resist (AZ 1512)",
        "description": "Achieves nominal 1.2 um film thickness at 4000 RPM, 30s.",
        "parameters": {
            "dispense_volume_ml": 2.5,
            "target_rpm": 4000.0,
            "acceleration_rpm_s": 2000.0,
            "spin_time_s": 30.0,
            "viscosity_cSt": 25.0
        }
    },
    "Hotplate": {
        "id": "REC-BAKE-01",
        "process_type": "Hotplate",
        "recipe_name": "Soft Bake Baseline",
        "description": "Drives off solvent to 5% residual content at 95 C for 60s.",
        "parameters": {
            "temperature_celsius": 95.0,
            "bake_time_s": 60.0,
            "exhaust_flow_cfm": 50.0
        }
    },
    "Exposure": {
        "id": "REC-EXPO-01",
        "process_type": "Exposure",
        "recipe_name": "i-line Contact Alignment",
        "description": "Nominal dose 120 mJ/cm2 under 365nm UV for 0.8 um critical dimension.",
        "parameters": {
            "wavelength_nm": 365.0,
            "target_dose_mj_cm2": 120.0,
            "exposure_time_s": 12.0,
            "numerical_aperture": 0.45
        }
    },
    "PlasmaEtcher": {
        "id": "REC-RIE-01",
        "process_type": "PlasmaEtcher",
        "recipe_name": "Anisotropic Silicon RIE",
        "description": "Reactive Ion Etching in SF6/O2 plasma: 300W RF bias, 20 mTorr, 50 sccm SF6.",
        "parameters": {
            "rf_bias_power_w": 300.0,
            "chamber_pressure_mtorr": 20.0,
            "sf6_flow_sccm": 50.0,
            "o2_flow_sccm": 5.0,
            "etch_duration_s": 45.0,
            "target_anisotropy": 0.92
        }
    }
}

@router.get("/")
def list_recipes():
    """List available standard baseline fabrication recipes."""
    return list(STANDARD_RECIPES.values())

@router.get("/{process_type}")
def get_recipe_by_tool(process_type: str):
    """Retrieve optimal process recipe for a specific equipment module."""
    recipe = STANDARD_RECIPES.get(process_type)
    if not recipe:
        raise HTTPException(status_code=404, detail=f"No baseline recipe found for {process_type}")
    return recipe
