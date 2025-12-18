# app.py
from fastapi import FastAPI, HTTPException
from fastapi.middleware.cors import CORSMiddleware
from pydantic import BaseModel
import io
import requests

# --- Image captioning dependencies ---
from PIL import Image, UnidentifiedImageError
from transformers import ViTImageProcessor, AutoTokenizer, VisionEncoderDecoderModel
import torch

# --- NLP scoring dependencies ---
import language_tool_python
from sentence_transformers import SentenceTransformer, util

# -----------------------------------------------------------------------------
# Khởi tạo FastAPI + CORS
# -----------------------------------------------------------------------------
app = FastAPI()

origins = [
    "http://localhost:4200",
    "http://localhost:7162",
    "http://localhost",
    "http://127.0.0.1",
]
app.add_middleware(
    CORSMiddleware,
    allow_origins=origins,
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

# -----------------------------------------------------------------------------
# Tải model cho IMAGE CAPTIONING (giữ nguyên logic từ api.py)
# -----------------------------------------------------------------------------
model_name = "nlpconnect/vit-gpt2-image-captioning"

try:
    feature_extractor = ViTImageProcessor.from_pretrained(model_name)
    tokenizer = AutoTokenizer.from_pretrained(model_name)
    if tokenizer.pad_token is None:
        tokenizer.pad_token = tokenizer.eos_token

    vision2text_model = VisionEncoderDecoderModel.from_pretrained(model_name)

    device = "cuda" if torch.cuda.is_available() else "cpu"
    vision2text_model.to(device)

    # Thiết lập tham số decoder giống bản gốc
    vision2text_model.config.decoder_start_token_id = getattr(tokenizer, "bos_token_id", None) or tokenizer.cls_token_id
    vision2text_model.config.eos_token_id = tokenizer.eos_token_id
    vision2text_model.config.pad_token_id = tokenizer.pad_token_id
    vision2text_model.config.vocab_size = vision2text_model.config.decoder.vocab_size

    print(f"[Caption] Loaded {model_name} on {device}")
except Exception as e:
    # Nếu không tải được model thì raise lỗi ngay khi khởi động
    raise RuntimeError(f"Failed to load caption model '{model_name}': {e}")

def generate_caption(image_input: Image.Image) -> str:
    """
    Sinh caption cho ảnh. Giữ nguyên tham số sinh từ api.py.
    """
    pixel_values = feature_extractor(images=image_input, return_tensors="pt").pixel_values.to(device)
    gen_kwargs = {"max_length": 16, "num_beams": 4}
    output_ids = vision2text_model.generate(pixel_values, **gen_kwargs)
    preds = tokenizer.batch_decode(output_ids, skip_special_tokens=True)
    return preds[0].strip()

# -----------------------------------------------------------------------------
# Tải công cụ cho NLP SCORING (giữ nguyên logic từ main.py)
# -----------------------------------------------------------------------------
grammar_tool = language_tool_python.LanguageTool('en-US')
semantic_model = SentenceTransformer('all-MiniLM-L6-v2')

# -----------------------------------------------------------------------------
# Pydantic models
# -----------------------------------------------------------------------------
class ScoreRequest(BaseModel):
    transcript: str
    sample_answer: str

class ScoreResponse(BaseModel):
    grammar_score: float
    content_score: float
    vocabulary_score: float

class CaptionRequest(BaseModel):
    imageUrl: str

class CaptionResponse(BaseModel):
    caption: str

# -----------------------------------------------------------------------------
# Endpoint: NLP Scoring (giữ nguyên thuật toán từ main.py)
# -----------------------------------------------------------------------------
@app.post("/score_nlp", response_model=ScoreResponse)
def score_natural_language_processing(request: ScoreRequest):
    """
    Enhanced TOEIC Speaking scoring aligned with ETS criteria.
    Scores Grammar, Vocabulary, and Content (Task Appropriateness).
    """
    transcript_text = request.transcript.strip() if request.transcript else ""
    sample_answer_text = request.sample_answer

    if not transcript_text:
        return ScoreResponse(grammar_score=0.0, content_score=0.0, vocabulary_score=0.0)

    words = transcript_text.split()
    word_count = len(words)
    
    # =========================================================================
    # 1. GRAMMAR SCORING - Enhanced with error classification & complexity
    # =========================================================================
    matches = grammar_tool.check(transcript_text)
    
    # 1.1 Classify errors by severity
    critical_errors = 0
    major_errors = 0
    minor_errors = 0
    
    critical_categories = ['SUBJECT_VERB_AGREEMENT', 'VERB_TENSE', 'VERB_FORM']
    major_categories = ['PREPOSITION', 'ARTICLE', 'PLURAL', 'POSSESSIVE']
    
    for match in matches:
        rule_id = match.ruleId
        category = match.category
        
        if any(cat in rule_id.upper() or cat in category.upper() for cat in critical_categories):
            critical_errors += 1
        elif any(cat in rule_id.upper() or cat in category.upper() for cat in major_categories):
            major_errors += 1
        else:
            minor_errors += 1
    
    # Weighted error count
    weighted_errors = critical_errors * 3 + major_errors * 2 + minor_errors * 1
    
    # 1.2 Analyze grammar complexity
    text_lower = transcript_text.lower()
    sentences = [s.strip() for s in transcript_text.replace('!', '.').replace('?', '.').split('.') if s.strip()]
    
    # Complex sentence markers
    complex_markers = ['although', 'though', 'even though', 'because', 'since', 'while', 'whereas', 'if', 'unless', 'until']
    compound_markers = [' and ', ' but ', ' or ', ' so ', ' yet ']
    
    complex_count = sum(1 for s in sentences if any(marker in s.lower() for marker in complex_markers))
    compound_count = sum(1 for s in sentences if any(marker in s.lower() for marker in compound_markers))
    
    # Passive voice detection (simple heuristic)
    passive_indicators = [' was ', ' were ', ' been ', ' being ']
    passive_count = sum(1 for ind in passive_indicators if ind in text_lower)
    
    # Complexity bonus (0-15 points)
    complexity_bonus = 0
    if len(sentences) > 0:
        complex_ratio = (complex_count + compound_count) / len(sentences)
        if complex_ratio >= 0.4:
            complexity_bonus = 15
        elif complex_ratio >= 0.2:
            complexity_bonus = 10
        elif complex_ratio >= 0.1:
            complexity_bonus = 5
    
    # Add passive voice bonus
    if passive_count >= 2:
        complexity_bonus = min(complexity_bonus + 5, 15)
    
    # 1.3 Calculate grammar score
    if word_count > 0:
        error_rate = (weighted_errors / word_count) * 100
        
        # ETS-aligned scoring thresholds
        if error_rate < 2:
            grammar_score = 95 + (2 - error_rate) * 2.5
        elif error_rate < 5:
            grammar_score = 80 + (5 - error_rate) * 5
        elif error_rate < 10:
            grammar_score = 60 + (10 - error_rate) * 4
        elif error_rate < 20:
            grammar_score = 30 + (20 - error_rate) * 3
        else:
            grammar_score = max(0, 30 - (error_rate - 20) * 1.5)
        
        # Apply complexity bonus
        grammar_score = min(100, grammar_score + complexity_bonus)
        grammar_score = round(max(0, min(100, grammar_score)), 2)
    else:
        grammar_score = 0.0

    # =========================================================================
    # 2. CONTENT SCORING - Enhanced with flexibility for creative answers
    # =========================================================================
    
    # 2.1 Semantic similarity (base score)
    emb1 = semantic_model.encode(transcript_text, convert_to_tensor=True)
    emb2 = semantic_model.encode(sample_answer_text, convert_to_tensor=True)
    cosine_similarity = util.cos_sim(emb1, emb2)
    similarity_score = float(cosine_similarity.item()) * 100
    
    # 2.2 Keyword matching for topic relevance (new!)
    # Extract important keywords from sample answer
    import re
    sample_clean = re.sub(r'[^\w\s]', '', sample_answer_text.lower())
    sample_word_list = sample_clean.split()
    
    # Remove common stop words
    stop_words = {
        'the', 'be', 'to', 'of', 'and', 'a', 'in', 'that', 'have', 'i',
        'it', 'for', 'not', 'on', 'with', 'he', 'as', 'you', 'do', 'at',
        'this', 'but', 'his', 'by', 'from', 'they', 'we', 'say', 'her', 'she',
        'or', 'an', 'will', 'my', 'one', 'all', 'would', 'there', 'their', 'what',
        'so', 'up', 'out', 'if', 'about', 'who', 'get', 'which', 'go', 'me',
        'when', 'make', 'can', 'like', 'time', 'no', 'just', 'him', 'know', 'take',
        'is', 'are', 'was', 'were', 'been', 'being', 'am'
    }
    
    # Get meaningful keywords from sample (nouns, verbs, adjectives)
    sample_keywords = [w for w in sample_word_list if w not in stop_words and len(w) > 3]
    
    # Check how many keywords appear in transcript
    transcript_clean = re.sub(r'[^\w\s]', '', transcript_text.lower())
    keyword_matches = sum(1 for kw in sample_keywords if kw in transcript_clean)
    
    # Calculate keyword coverage
    if len(sample_keywords) > 0:
        keyword_coverage = keyword_matches / len(sample_keywords)
    else:
        keyword_coverage = 0
    
    # Keyword bonus (0-20 points) - rewards topic relevance even with different words
    if keyword_coverage >= 0.5:  # 50%+ keywords covered
        keyword_bonus = 20
    elif keyword_coverage >= 0.3:  # 30-50%
        keyword_bonus = 15
    elif keyword_coverage >= 0.15:  # 15-30%
        keyword_bonus = 10
    elif keyword_coverage >= 0.05:  # 5-15%
        keyword_bonus = 5
    else:
        keyword_bonus = 0
    
    # 2.3 Completeness check (length-based)
    sample_words = sample_answer_text.split()
    sample_length = len(sample_words)
    
    if sample_length > 0:
        length_ratio = word_count / sample_length
        
        # Penalize if too short or too long
        if 0.8 <= length_ratio <= 1.5:
            completeness_score = 100
        elif 0.5 <= length_ratio < 0.8 or 1.5 < length_ratio <= 2.0:
            completeness_score = 80
        elif 0.3 <= length_ratio < 0.5 or 2.0 < length_ratio <= 2.5:
            completeness_score = 60
        else:
            completeness_score = 40
    else:
        completeness_score = 100
    
    # 2.4 Discourse markers detection (organization bonus)
    discourse_markers = {
        'sequencing': ['first', 'second', 'third', 'finally', 'lastly', 'next', 'then'],
        'addition': ['in addition', 'furthermore', 'moreover', 'also', 'besides'],
        'contrast': ['however', 'on the other hand', 'although', 'but', 'yet', 'nevertheless'],
        'conclusion': ['in conclusion', 'to sum up', 'overall', 'in summary', 'therefore']
    }
    
    discourse_count = 0
    for category, markers in discourse_markers.items():
        for marker in markers:
            if marker in text_lower:
                discourse_count += 1
                break  # Count each category only once
    
    discourse_bonus = min(discourse_count * 3, 12)  # Max 12 points bonus (increased)
    
    # 2.5 Opinion/Response quality markers (for speaking tasks)
    opinion_markers = ['i think', 'i believe', 'in my opinion', 'from my perspective', 'i feel']
    reasoning_markers = ['because', 'therefore', 'so', 'thus', 'as a result', 'consequently']
    example_markers = ['for example', 'for instance', 'such as', 'like']
    
    has_opinion = any(marker in text_lower for marker in opinion_markers)
    has_reasoning = any(marker in text_lower for marker in reasoning_markers)
    has_example = any(marker in text_lower for marker in example_markers)
    
    quality_bonus = 0
    if has_opinion:
        quality_bonus += 6  # Increased from 5
    if has_reasoning:
        quality_bonus += 4  # Increased from 3
    if has_example:
        quality_bonus += 3  # Increased from 2
    
    # 2.6 Calculate final content score
    # NEW WEIGHTS: Similarity 50%, Completeness/Quality 50%
    content_score = (
        similarity_score * 0.50 +                           # Semantic relevance (50% - reduced)
        completeness_score * 0.25 +                         # Completeness (25% - increased)
        (keyword_bonus + discourse_bonus + quality_bonus)   # Topic + Organization + Quality (25%)
    )
    content_score = round(min(100, max(0, content_score)), 2)

    # =========================================================================
    # 3. VOCABULARY SCORING - Enhanced with frequency analysis
    # =========================================================================
    
    if not words:
        vocabulary_score = 0.0
    else:
        # Clean words (remove punctuation)
        import re
        clean_words = [re.sub(r'[^\w]', '', w).lower() for w in words if re.sub(r'[^\w]', '', w)]
        
        # 3.1 Word Frequency Analysis (replaces character-based metrics)
        # Using approximate TOEIC/Academic word levels
        
        # Common TOEIC business/workplace words (higher tier)
        business_words = {
            'meeting', 'project', 'deadline', 'schedule', 'client', 'customer',
            'department', 'manager', 'colleague', 'presentation', 'budget',
            'report', 'conference', 'appointment', 'responsibility', 'opportunity',
            'experience', 'qualification', 'position', 'application', 'document',
            'agreement', 'contract', 'invoice', 'payment', 'delivery'
        }
        
        # Academic/Advanced words (approximate 5000-10000 frequency)
        academic_words = {
            'analyze', 'approach', 'assess', 'concept', 'consist', 'constitute',
            'context', 'define', 'demonstrate', 'derive', 'distribute', 'establish',
            'evaluate', 'evident', 'identify', 'indicate', 'interpret', 'involve',
            'maintain', 'obtain', 'occur', 'participate', 'perceive', 'require',
            'significant', 'similar', 'specific', 'strategy', 'structure', 'theory'
        }
        
        # Very common words (should be minimized for higher scores)
        basic_words = {
            'the', 'be', 'to', 'of', 'and', 'a', 'in', 'that', 'have', 'i',
            'it', 'for', 'not', 'on', 'with', 'he', 'as', 'you', 'do', 'at',
            'this', 'but', 'his', 'by', 'from', 'they', 'we', 'say', 'her', 'she',
            'or', 'an', 'will', 'my', 'one', 'all', 'would', 'there', 'their', 'what'
        }
        
        business_count = sum(1 for w in clean_words if w in business_words)
        academic_count = sum(1 for w in clean_words if w in academic_words)
        basic_count = sum(1 for w in clean_words if w in basic_words)
        
        # Calculate percentages
        if clean_words:
            business_ratio = business_count / len(clean_words)
            academic_ratio = academic_count / len(clean_words) 
            basic_ratio = basic_count / len(clean_words)
        else:
            business_ratio = academic_ratio = basic_ratio = 0
        
        # Frequency-based score (40%)
        freq_score = 50  # Base score
        freq_score += min(business_ratio * 200, 30)  # Up to +30 for business vocab
        freq_score += min(academic_ratio * 200, 20)  # Up to +20 for academic vocab
        freq_score -= min(basic_ratio * 50, 20)      # Penalty for too many basic words
        freq_score = min(100, max(0, freq_score))
        
        # 3.2 Lexical Diversity (30%) - Type-Token Ratio
        unique_words = set(clean_words)
        diversity_ratio = len(unique_words) / len(clean_words) if clean_words else 0
        
        if diversity_ratio >= 0.7:
            diversity_score = 90 + (diversity_ratio - 0.7) * 33
        elif diversity_ratio >= 0.5:
            diversity_score = 70 + (diversity_ratio - 0.5) * 100
        elif diversity_ratio >= 0.3:
            diversity_score = 40 + (diversity_ratio - 0.3) * 150
        else:
            diversity_score = diversity_ratio * 133
        diversity_score = min(100, max(0, diversity_score))
        
        # 3.3 Collocations & Phrasal Verbs (20%)
        common_collocations = [
            'make decision', 'take responsibility', 'carry out', 'put forward',
            'bring up', 'look forward to', 'get along', 'work out', 'find out',
            'take place', 'make sense', 'pay attention', 'keep in mind',
            'deal with', 'focus on', 'depend on', 'participate in'
        ]
        
        collocation_count = sum(1 for coll in common_collocations if coll in text_lower)
        collocation_score = min(collocation_count * 15, 100)
        
        # 3.4 Word Length Distribution (10%)
        avg_len = sum(len(w) for w in clean_words) / len(clean_words) if clean_words else 0
        
        if avg_len >= 5.5:
            length_score = 95 + (avg_len - 5.5) * 10
        elif avg_len >= 4.5:
            length_score = 70 + (avg_len - 4.5) * 50
        elif avg_len >= 3.5:
            length_score = 40 + (avg_len - 3.5) * 60
        else:
            length_score = avg_len * 10
        length_score = min(100, max(0, length_score))
        
        # Combine all vocabulary components
        vocabulary_score = (
            freq_score * 0.40 +
            diversity_score * 0.30 +
            collocation_score * 0.20 +
            length_score * 0.10
        )
        vocabulary_score = round(max(0, min(100, vocabulary_score)), 2)

    return ScoreResponse(
        grammar_score=grammar_score,
        content_score=content_score,
        vocabulary_score=vocabulary_score
    )

# -----------------------------------------------------------------------------
# Endpoint: Image Caption (giữ nguyên hành vi từ api.py)
# -----------------------------------------------------------------------------
@app.post("/caption", response_model=CaptionResponse)
def get_image_caption(body: CaptionRequest):
    image_url = body.imageUrl
    if not image_url:
        raise HTTPException(status_code=400, detail="imageUrl is required")

    try:
        resp = requests.get(image_url, stream=True, timeout=20)
        resp.raise_for_status()

        image = Image.open(io.BytesIO(resp.content)).convert("RGB")
        caption_text = generate_caption(image)
        return CaptionResponse(caption=caption_text)

    except requests.exceptions.RequestException as e:
        # Phản hồi giống api.py: 500 khi tải ảnh lỗi
        raise HTTPException(status_code=500, detail=f"Failed to download image from URL: {e}")
    except UnidentifiedImageError:
        # Phản hồi 400 khi URL không phải ảnh hợp lệ
        raise HTTPException(status_code=400, detail="The provided URL does not point to a valid image.")
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"An unexpected error occurred: {e}")
