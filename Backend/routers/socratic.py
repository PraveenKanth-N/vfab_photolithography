from fastapi import APIRouter, Depends
from sqlalchemy.orm import Session
from typing import Dict

from database import get_db
import models
import schemas

router = APIRouter(prefix="/socratic", tags=["Socratic AI Pedagogical Engine"])

# Standard BKT Model Parameters
P_GUESS = 0.20
P_SLIP = 0.10
P_TRANSIT = 0.15

# In-memory student mastery map: { (student_id, concept_id): mastery_score }
_student_mastery_cache: Dict[str, float] = {}

@router.post("/assess", response_model=schemas.SocraticMasteryResponse)
def evaluate_student_response(query: schemas.SocraticQuery, db: Session = Depends(get_db)):
    """
    Computes Bayesian Knowledge Tracing (BKT Eq. 8) update following student response 
    to a 3-tier Socratic dialogue prompt.
    """
    cache_key = f"{query.student_id}:{query.concept_id}"
    prior = _student_mastery_cache.get(cache_key, 0.50)

    # Calculate posterior given observation
    if query.response_correct:
        numerator = prior * (1.0 - P_SLIP)
        denominator = (prior * (1.0 - P_SLIP)) + ((1.0 - prior) * P_GUESS)
        p_l_given_obs = numerator / max(1e-6, denominator)
    else:
        numerator = prior * P_SLIP
        denominator = (prior * P_SLIP) + ((1.0 - prior) * (1.0 - P_GUESS))
        p_l_given_obs = numerator / max(1e-6, denominator)

    # Incorporate transition probability (learning gain)
    posterior = p_l_given_obs + (1.0 - p_l_given_obs) * P_TRANSIT
    posterior = max(0.01, min(0.99, posterior))
    _student_mastery_cache[cache_key] = posterior

    # Log assessment to database
    record = models.SocraticAssessment(
        session_id=query.session_id,
        student_id=query.student_id,
        concept_id=query.concept_id,
        prior_mastery=prior,
        posterior_mastery=posterior,
        response_correct=query.response_correct
    )
    db.add(record)
    db.commit()

    if posterior >= 0.85:
        recommendation = "Concept Mastered. Ready to proceed to next fabrication module."
    elif posterior >= 0.60:
        recommendation = "Developing Competency. Practice parameter boundary adjustments."
    else:
        recommendation = "Review Required. Socratic remediation prompts active."

    return schemas.SocraticMasteryResponse(
        student_id=query.student_id,
        concept_id=query.concept_id,
        prior_mastery=round(prior, 4),
        posterior_mastery=round(posterior, 4),
        recommendation=recommendation
    )

@router.get("/mastery/{student_id}")
def get_student_mastery(student_id: str):
    """Retrieve full concept mastery breakdown for a student."""
    concepts = ["SpinDynamics", "ThermalSolventEvaporation", "DillPhotokinetics", "PlasmaIonBombardment"]
    mastery = {}
    for c in concepts:
        key = f"{student_id}:{c}"
        mastery[c] = round(_student_mastery_cache.get(key, 0.50), 3)
    return {"student_id": student_id, "concepts": mastery}
