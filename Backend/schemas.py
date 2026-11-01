"""
Pydantic validation schemas for Semiconductor Digital Twin backend API.
"""

from pydantic import BaseModel, Field
from typing import Optional, List, Dict, Any
from datetime import datetime

# --- Session Schemas ---
class SessionCreate(BaseModel):
    student_id: str = Field(..., example="STUDENT_23MIC0078")
    module_name: str = Field(default="Photolithography_Etching", example="Photolithography_Etching")

class SessionComplete(BaseModel):
    yield_percentage: float = Field(default=100.0, ge=0.0, le=100.0)

class SessionResponse(BaseModel):
    session_id: str
    student_id: str
    module_name: str
    status: str
    start_time: datetime
    end_time: Optional[datetime] = None
    final_yield: float

    class Config:
        from_attributes = True


# --- Wafer Schemas ---
class WaferCreate(BaseModel):
    session_id: Optional[str] = None
    wafer_id: str = Field(default="WAF-001")
    state: str = Field(..., example="ResistCoated")
    revision: int = Field(default=0)
    diameter: float = Field(default=100.0)
    thickness: float = Field(default=0.5)
    substrate_material: str = Field(default="Silicon")
    crystal_orientation: str = Field(default="<100>")
    surface_state: str = Field(default="Clean")
    defect_count: int = Field(default=0)

class WaferResponse(BaseModel):
    id: int
    session_id: Optional[str]
    wafer_id: str
    state: str
    revision: int
    diameter: float
    thickness: float
    substrate_material: str
    crystal_orientation: str
    surface_state: str
    defect_count: int
    timestamp: datetime

    class Config:
        from_attributes = True


# --- Telemetry Schemas ---
class TelemetryCreate(BaseModel):
    session_id: Optional[str] = None
    equipment_name: str = Field(..., example="PlasmaEtcher")
    parameter_name: str = Field(..., example="RF_Power")
    parameter_value: float = Field(..., example=300.0)
    unit: Optional[str] = Field(default="W")

class TelemetryResponse(BaseModel):
    id: int
    session_id: Optional[str]
    equipment_name: str
    parameter_name: float
    parameter_value: float
    unit: str
    timestamp: datetime

    class Config:
        from_attributes = True


# --- Defect Schemas ---
class DefectCreate(BaseModel):
    session_id: Optional[str] = None
    wafer_id: str = Field(default="WAF-001")
    defect_id: str = Field(..., example="DEF-001")
    defect_type: str = Field(..., example="ResistStriation")
    severity: float = Field(default=0.5, ge=0.0, le=1.0)
    is_critical: bool = Field(default=False)
    origin_equipment: str = Field(default="SpinCoater")
    pos_x: float = Field(default=0.0)
    pos_y: float = Field(default=0.0)
    pos_z: float = Field(default=0.0)

class DefectResponse(BaseModel):
    id: int
    session_id: Optional[str]
    wafer_id: str
    defect_id: str
    defect_type: str
    severity: float
    is_critical: bool
    origin_equipment: str
    pos_x: float
    pos_y: float
    pos_z: float
    timestamp: datetime

    class Config:
        from_attributes = True

class DefectSummary(BaseModel):
    total_defects: int
    critical_defects: int
    yield_estimate_percentage: float
    defects_by_type: Dict[str, int]
    defects_by_equipment: Dict[str, int]


# --- Recipe Schemas ---
class RecipeCreate(BaseModel):
    id: str = Field(..., example="REC-LITHO-STD")
    process_type: str = Field(..., example="SpinCoater")
    recipe_name: str = Field(..., example="Standard Positive Resist 1.2um")
    description: Optional[str] = ""
    parameters: Dict[str, Any]

class RecipeResponse(BaseModel):
    id: str
    process_type: str
    recipe_name: str
    description: str
    parameters: Dict[str, Any]
    created_at: datetime


# --- Socratic Assessment Schemas ---
class SocraticQuery(BaseModel):
    session_id: Optional[str] = None
    student_id: str
    concept_id: str
    response_correct: bool

class SocraticMasteryResponse(BaseModel):
    student_id: str
    concept_id: str
    prior_mastery: float
    posterior_mastery: float
    recommendation: str
