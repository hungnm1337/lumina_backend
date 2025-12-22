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

# CRITICAL: Configure for spoken language (less strict than written)
# Disable overly formal rules that flag natural speech
grammar_tool.disabled_rules = {
    'SENT_START_NUM',              # "2 people are..." OK in speaking
    'WHITESPACE_RULE',             # Less critical in transcripts
    'EN_QUOTES',                   # Quote formatting not critical
    'EN_UNPAIRED_BRACKETS',        # Transcripts may be incomplete
}

semantic_model = SentenceTransformer('all-MiniLM-L6-v2')

# -----------------------------------------------------------------------------
# Pydantic models
# -----------------------------------------------------------------------------
class ScoreRequest(BaseModel):
    transcript: str
    sample_answer: str
    question: str = ""  # NEW: Question text for QA relevance detection
    part_code: str = None  # Optional: e.g., "SPEAKING_PART_1" for Read Aloud

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
    
    For Part 1 (Read Aloud): Uses TEXT MATCHING instead of semantic similarity.
    The transcript should match the given text exactly to get a high content score.
    """
    transcript_text = request.transcript.strip() if request.transcript else ""
    sample_answer_text = request.sample_answer
    part_code = (request.part_code or "").upper()
    
    # Check if this is Part 1 (Read Aloud) - uses different scoring
    is_read_aloud = part_code == "SPEAKING_PART_1"

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
    
    # 1.2 CRITICAL FIX: Detect fragmented/incomplete responses
    # LanguageTool often misses errors in very short fragments
    sentences = [s.strip() for s in transcript_text.replace('!', '.').replace('?', '.').split('.') if s.strip()]
    
    # Count very short fragments (< 4 words) - likely incomplete
    fragment_count = sum(1 for s in sentences if len(s.split()) < 4)
    fragment_ratio = fragment_count / max(len(sentences), 1)
    
    # Count filler words and hesitations
    filler_words = ['uh', 'um', 'mmm', 'hmm', 'er', 'ah']
    filler_count = sum(transcript_text.lower().count(f' {filler} ') + 
                       transcript_text.lower().count(f'{filler} ') + 
                       transcript_text.lower().count(f' {filler}')
                       for filler in filler_words)
    
    # Penalty for fragmented responses
    fragment_penalty = 0
    if fragment_ratio > 0.5:  # More than 50% fragments
        fragment_penalty = 30
    elif fragment_ratio > 0.3:
        fragment_penalty = 15
    
    # Penalty for excessive fillers (normalized by word count)
    filler_penalty = 0
    if word_count > 0:
        filler_rate = (filler_count / word_count) * 100
        if filler_rate > 20:  # > 20% fillers
            filler_penalty = 20
        elif filler_rate > 10:
            filler_penalty = 10
    
    # 1.3 Analyze grammar complexity
    text_lower = transcript_text.lower()
    
    # Complex sentence markers
    complex_markers = ['although', 'though', 'even though', 'because', 'since', 'while', 'whereas', 'if', 'unless', 'until']
    compound_markers = [' and ', ' but ', ' or ', ' so ', ' yet ']
    
    complex_count = sum(1 for s in sentences if any(marker in s.lower() for marker in complex_markers))
    compound_count = sum(1 for s in sentences if any(marker in s.lower() for marker in compound_markers))
    
    # Passive voice detection (simple heuristic)
    passive_indicators = [' was ', ' were ', ' been ', ' being ']
    passive_count = sum(1 for ind in passive_indicators if ind in text_lower)
    
    # Complexity bonus (0-15 points) - BUT only if response has substance
    complexity_bonus = 0
    if len(sentences) > 0 and fragment_ratio < 0.3:  # Don't reward fragments
        complex_ratio = (complex_count + compound_count) / len(sentences)
        if complex_ratio >= 0.4:
            complexity_bonus = 15
        elif complex_ratio >= 0.2:
            complexity_bonus = 10
        elif complex_ratio >= 0.1:
            complexity_bonus = 5
    
    # Add passive voice bonus (only if not fragmented)
    if passive_count >= 2 and fragment_ratio < 0.3:
        complexity_bonus = min(complexity_bonus + 5, 15)
    
    # 1.4 Calculate grammar score with penalties
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
        
        # Apply fragment and filler penalties
        grammar_score = grammar_score - fragment_penalty - filler_penalty
        
        grammar_score = round(max(0, min(100, grammar_score)), 2)
    else:
        grammar_score = 0.0

    # =========================================================================
    # 2. CONTENT SCORING - Enhanced with flexibility for creative answers
    # =========================================================================
    
    # 2.1 For Part 1 (Read Aloud): Use TEXT MATCHING instead of semantic similarity
    # ETS Scoring: Measures how well test taker reads the given text
    # - Score 3 (83-100): Highly intelligible, minor lapses (≥80% coverage)
    # - Score 2 (50-83): Generally intelligible, some lapses (50-80% coverage)
    # - Score 1 (17-50): Intelligible at times, significant gaps (30-50% coverage)
    # - Score 0 (0-17): No response or completely unrelated (<30% coverage)
    if is_read_aloud:
        import re
        # Normalize both texts for comparison
        sample_normalized = re.sub(r'[^\w\s]', '', sample_answer_text.lower())
        transcript_normalized = re.sub(r'[^\w\s]', '', transcript_text.lower())
        
        sample_words = sample_normalized.split()
        transcript_words = transcript_normalized.split()
        
        if not sample_words:
            content_score = 0.0
        else:
            # Calculate word-level matching (words from sample that appear in transcript)
            matched_words = 0
            sample_word_set = set(sample_words)
            
            for word in transcript_words:
                if word in sample_word_set:
                    matched_words += 1
            
            # Coverage: what % of SAMPLE words appear in transcript
            # This allows partial credit when user reads only part of the text
            coverage = matched_words / len(sample_words) if sample_words else 0
            
            # Order accuracy: check if words appear in roughly correct order
            # Using consecutive pair matching
            if len(transcript_words) > 1 and len(sample_words) > 1:
                order_matches = 0
                for i, word in enumerate(transcript_words[:-1]):
                    if word in sample_word_set:
                        next_word = transcript_words[i + 1]
                        if next_word in sample_word_set:
                            order_matches += 1
                # Normalize by expected pairs in transcript
                max_possible_pairs = min(len(transcript_words) - 1, len(sample_words) - 1)
                order_ratio = order_matches / max(max_possible_pairs, 1)
            else:
                order_ratio = 0 if len(transcript_words) <= 1 else 0.5
            
            # Calculate content score aligned with ETS 0-3 scale
            # Mapping: 0→0-16.67, 1→16.68-50, 2→50.01-83, 3→83.01-100
            
            if coverage >= 0.80:
                # Score 3: Highly intelligible, near-complete reading
                # 83-100 on 100-point scale
                base_score = 83 + (coverage - 0.80) * 85  # 83-100
                order_bonus = order_ratio * 10  # Up to +10 for good order
                content_score = min(100, base_score + order_bonus)
                
            elif coverage >= 0.50:
                # Score 2: Generally intelligible, more than half read correctly
                # 50-83 on 100-point scale
                # Linear interpolation: 50% coverage → 50 points, 80% → 83 points
                base_score = 50 + (coverage - 0.50) * 110  # 50-83
                order_bonus = order_ratio * 8  # Up to +8 for good order
                content_score = min(83, base_score + order_bonus)
                
            elif coverage >= 0.30:
                # Score 1: Intelligible at times, significant gaps but trying
                # 17-50 on 100-point scale
                base_score = 17 + (coverage - 0.30) * 165  # 17-50
                order_bonus = order_ratio * 5  # Up to +5 for good order
                content_score = min(50, base_score + order_bonus)
                
            else:
                # Score 0: Less than 30% coverage - likely unrelated or no real attempt
                # 0-17 on 100-point scale (will trigger Content Gate)
                if coverage > 0:
                    content_score = coverage * 56.67  # 0-17
                else:
                    content_score = 0
            
            content_score = round(min(100, max(0, content_score)), 2)
        
        # For Read Aloud, grammar and vocabulary are not evaluated by NLP
        # (pronunciation/intonation handled by Azure Speech)
        grammar_score = 0.0
        vocabulary_score = 0.0
        
        return ScoreResponse(
            grammar_score=grammar_score,
            content_score=content_score,
            vocabulary_score=vocabulary_score
        )
    
    # =========================================================================
    # 2.2 For other parts: 4-DIMENSIONAL INTELLIGENT CONTENT SCORING
    # =========================================================================
    # CRITICAL ENHANCEMENT: Don't just compare with sample answer!
    # Compare with QUESTION to detect:
    # - Creative correct answers (different from sample but answers question)
    # - Keyword parroting (mentions sample keywords but doesn't answer)
    
    question_text = request.question or sample_answer_text  # Fallback to sample if no question
    
    # =================================================================
    # DIMENSION 1: Question-Answer Relevance (40% - MOST IMPORTANT!)
    # =================================================================
    # Does the transcript actually ANSWER the question asked?
    emb_question = semantic_model.encode(question_text, convert_to_tensor=True)
    emb_transcript = semantic_model.encode(transcript_text, convert_to_tensor=True)
    
    qa_relevance = util.cos_sim(emb_question, emb_transcript)
    qa_relevance_score = float(qa_relevance.item()) * 100
    
    # 🔍 DEBUG LOG
    print(f"\n[NLP DEBUG] Question: {question_text[:100]}...")
    print(f"[NLP DEBUG] Transcript: {transcript_text[:100]}...")
    print(f"[NLP DEBUG] QA Relevance Score: {qa_relevance_score:.2f}")
    
    # Off-topic penalty for completely irrelevant responses
    off_topic_penalty = 0
    if qa_relevance_score < 20:
        # Transcript has NOTHING to do with question
        off_topic_penalty = 30
        print(f"[NLP DEBUG] ❌ Off-topic penalty: {off_topic_penalty} (score < 20)")
    elif qa_relevance_score < 35:
        # Marginally related but doesn't really answer
        off_topic_penalty = 15
        print(f"[NLP DEBUG] ⚠️  Off-topic penalty: {off_topic_penalty} (score < 35)")
    
    # =================================================================
    # DIMENSION 2: Sample Answer Similarity (30% - Reference Quality)
    # =================================================================
    # How similar to the expected answer style/content?
    # This is for reference only, NOT required to match exactly
    emb_sample = semantic_model.encode(sample_answer_text, convert_to_tensor=True)
    sample_similarity = util.cos_sim(emb_transcript, emb_sample)
    sample_similarity_score = float(sample_similarity.item()) * 100
    
    # =================================================================
    # DIMENSION 3: Question Keyword Coverage (15%)
    # =================================================================
    # Extract keywords FROM QUESTION to check if answer is on-topic
    import re
    
    def extract_keywords(text):
        question_words = {'what', 'when', 'where', 'who', 'why', 'how', 'which', 
                          'do', 'does', 'did', 'is', 'are', 'was', 'were', 'can'}
        stop_words = {'the', 'be', 'to', 'of', 'and', 'a', 'in', 'that', 'have',
                      'it', 'for', 'not', 'on', 'with', 'as', 'you', 'at'}
        words = re.sub(r'[^\w\s]', '', text.lower()).split()
        return [w for w in words if w not in question_words and w not in stop_words and len(w) > 3]
    
    question_keywords = extract_keywords(question_text)
    transcript_lower = transcript_text.lower()
    
    if question_keywords:
        keyword_matches = sum(1 for kw in question_keywords if kw in transcript_lower)
        keyword_coverage_score = (keyword_matches / len(question_keywords)) * 100
    else:
        keyword_coverage_score = 50 # Neutral
    
    # DIMENSION 4: Completeness score (will compute later with discourse bonuses)
    
    # ===================================================================
    # 2.4 CRITICAL FIX: Part-Specific Word Count Requirements
    # ===================================================================
    # Different parts have different length expectations
    min_word_count_penalty = 0
    
    if not is_read_aloud:  # Only for Parts 2-5
        if part_code == "SPEAKING_PART_2":
            # Part 2: Picture description (20-30 words optimal)
            if word_count < 10:
                min_word_count_penalty = 40  # Severe
            elif word_count < 15:
                min_word_count_penalty = 25
            elif word_count < 20:
                min_word_count_penalty = 15
            elif word_count < 30:
                min_word_count_penalty = 5   # Light - still acceptable
                
        elif part_code == "SPEAKING_PART_3":
            # Part 3: Short answer questions (15-25 words optimal)
            # Questions 5-6 can be concise if answering directly  
            if word_count < 8:
                min_word_count_penalty = 40
            elif word_count < 12:
                min_word_count_penalty = 20
            elif word_count < 18:
                min_word_count_penalty = 10
                
        elif part_code == "SPEAKING_PART_4":
            # Part 4: Information-based (20-30 words)
            # Can be concise if info is accurate
            if word_count < 10:
                min_word_count_penalty = 40
            elif word_count < 15:
                min_word_count_penalty = 20
            elif word_count < 25:
                min_word_count_penalty = 10
                
        elif part_code == "SPEAKING_PART_5":
            # Part 5: Opinion (40-60 words)
            # Needs reasoning + examples
            if word_count < 20:
                min_word_count_penalty = 40
            elif word_count < 30:
                min_word_count_penalty = 25
            elif word_count < 40:
                min_word_count_penalty = 15
            elif word_count < 50:
                min_word_count_penalty = 5
        else:
            # Default for any other parts
            if word_count < 15:
                min_word_count_penalty = 40
            elif word_count < 25:
                min_word_count_penalty = 20
            elif word_count < 35:
                min_word_count_penalty = 10
    
    # 2.5 Discourse markers detection (organization bonus)
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
    
    
    # =================================================================
    # Calculate DIMENSION 4: Completeness Score (15%)
    # =================================================================
    sample_words = sample_answer_text.split()
    sample_length = len(sample_words)
    
    # CRITICAL FIX: Handle empty or very short sample answers
    # For Part 2-5, sample should always exist. If not, it's a data issue.
    if sample_length > 0:
        length_ratio = word_count / sample_length
        
        # Be STRICTER for very short answers
        if length_ratio < 0.15:
            # Less than 15% of sample length → Very incomplete
            base_completeness = 10
        elif 0.15 <= length_ratio < 0.3:
            # 15-30% of sample → Incomplete
            base_completeness = 30
        elif 0.3 <= length_ratio < 0.5:
            # 30-50% of sample → Partially complete
            base_completeness = 50
        elif 0.5 <= length_ratio < 0.8:
            # 50-80% of sample → Mostly complete
            base_completeness = 75
        elif 0.8 <= length_ratio <= 1.5:
            # 80-150% of sample → Complete
            base_completeness = 100
        elif 1.5 < length_ratio <= 2.0:
            # Too long but acceptable
            base_completeness = 80
        else:
            # Way too long → Rambling
            base_completeness = 60
    else:
        # No sample answer → Cannot judge completeness
        # Use word count as rough guide
        if word_count < 15:
            base_completeness = 20
        elif word_count < 30:
            base_completeness = 50
        else:
            base_completeness = 75
    
    # Add discourse and quality bonuses to completeness
    completeness_score = min(100, base_completeness + discourse_bonus + quality_bonus)
    
    # =================================================================
    # FINAL 4-DIMENSIONAL CONTENT SCORE
    # =================================================================
    # CRITICAL FIX FOR PART 2: Pictures can be described many ways
    # Reduce sample similarity weight, increase QA relevance for Part 2
    if part_code == "SPEAKING_PART_2":
        # Part 2: Describe a Picture
        # Sample answer is just ONE way to describe, user can describe differently
        content_score = (
            qa_relevance_score * 0.50 +        # Increase: Does desc match picture? (50%)
            sample_similarity_score * 0.20 +    # Decrease: Sample is just reference (20%)
            keyword_coverage_score * 0.15 +     # mentions key elements (15%)
            completeness_score * 0.15           # Sufficient detail (15%)
        )
    else:
        # Part 3, 4, 5: Standard weights
        content_score = (
            qa_relevance_score * 0.40 +        # DIMENSION 1: Answers THE QUESTION? (40%)
            sample_similarity_score * 0.30 +   # DIMENSION 2: Similar to sample? (30%)
            keyword_coverage_score * 0.15 +    # DIMENSION 3: Mentions concepts? (15%)
            completeness_score * 0.15          # DIMENSION 4: Sufficient detail? (15%)
        )
    
    # 🔍 DEBUG LOG - Show all dimensions
    print(f"\n[NLP DEBUG] ========== CONTENT SCORE BREAKDOWN ==========")
    print(f"[NLP DEBUG] Part Code: {part_code}")
    print(f"[NLP DEBUG] Word Count: {word_count}")
    print(f"[NLP DEBUG] Dimension 1 - QA Relevance: {qa_relevance_score:.2f}")
    print(f"[NLP DEBUG] Dimension 2 - Sample Similarity: {sample_similarity_score:.2f}")
    print(f"[NLP DEBUG] Dimension 3 - Keyword Coverage: {keyword_coverage_score:.2f}")
    print(f"[NLP DEBUG] Dimension 4 - Completeness: {completeness_score:.2f}")
    print(f"[NLP DEBUG] Off-topic Penalty: -{off_topic_penalty}")
    print(f"[NLP DEBUG] Word Count Penalty: -{min_word_count_penalty}")
    print(f"[NLP DEBUG] Content (before penalties): {content_score:.2f}")
    
    # Apply off-topic penalty (from Dimension 1)
    content_score = content_score - off_topic_penalty
    
    # Apply minimum word count penalty
    content_score = content_score - min_word_count_penalty
    
    content_score = round(min(100, max(0, content_score)), 2)
    
    print(f"[NLP DEBUG] ========== FINAL CONTENT SCORE: {content_score:.2f} ==========\n")

    # =========================================================================
    # 3. VOCABULARY SCORING - CRITICAL FIX: Use wordfreq instead of hardcoded lists
    # =========================================================================
    
    if not words:
        vocabulary_score = 0.0
    else:
        # Clean words (remove punctuation)
        import re
        from wordfreq import word_frequency, zipf_frequency
        
        clean_words = [re.sub(r'[^\w]', '', w).lower() for w in words if re.sub(r'[^\w]', '', w)]
        
        # ===============================================================
        # 3.1 INTELLIGENT Word Difficulty Analysis using wordfreq
        # ===============================================================
        # Zipf scale: 1-7 (7 = very common like "the", 1 = very rare/academic)
        # TOEIC high scores need diverse, less common vocabulary
        
        word_scores = []
        advanced_count = 0
        business_count = 0
        intermediate_count = 0
        
        for word in clean_words:
            if len(word) <= 2:  # Skip very short words
                continue
                
            zipf = zipf_frequency(word, 'en')
            
            # Categorize by Zipf frequency
            if zipf < 3.5:  # Very rare (academic/technical)
                word_scores.append(100)
                advanced_count += 1
            elif zipf < 4.5:  # Uncommon (business/advanced)
                word_scores.append(85)
                business_count += 1
            elif zipf < 5.5:  # Intermediate
                word_scores.append(65)
                intermediate_count += 1
            else:  # Common (basic words)
                word_scores.append(40)
        
        # Calculate frequency-based score (50%)
        if word_scores:
            freq_score = sum(word_scores) / len(word_scores)
        else:
            freq_score = 50  # Neutral
        
        # ===============================================================
        # 3.2 Lexical Diversity (30%) - Type-Token Ratio
        # ===============================================================
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
