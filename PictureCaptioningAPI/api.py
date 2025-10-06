from flask import Flask, request, jsonify
from PIL import Image
import requests
import io

# Import các thành phần cụ thể cho mô hình VisionEncoderDecoderModel
from transformers import ViTImageProcessor, AutoTokenizer, VisionEncoderDecoderModel

app = Flask(__name__)

# --- CẤU HÌNH VÀ TẢI MÔ HÌNH CỦA BẠN TẠY ĐÂY ---
model_name = "nlpconnect/vit-gpt2-image-captioning"

try:
    # Tải bộ xử lý ảnh (Feature Extractor)
    # Bộ xử lý này sẽ chuẩn bị ảnh (resize, normalize) cho mô hình
    feature_extractor = ViTImageProcessor.from_pretrained(model_name)
    
    # Tải bộ mã hóa/giải mã văn bản (Tokenizer)
    # Dùng để chuyển đổi văn bản thành token ID và ngược lại
    tokenizer = AutoTokenizer.from_pretrained(model_name)
    
    # Tải mô hình VisionEncoderDecoderModel
    # Đây là mô hình chính kết hợp Vision Transformer (ViT) và GPT-2
    model = VisionEncoderDecoderModel.from_pretrained(model_name)
    
    # Cấu hình một số tham số tạo văn bản cho mô hình
    # Đây là các tham số giúp mô hình tạo ra chú thích tốt hơn
    model.config.decoder_start_token_id = tokenizer.cls_token_id
    model.config.pad_token_id = tokenizer.pad_token_id
    model.config.vocab_size = model.config.decoder.vocab_size
    
    # Nếu có GPU, di chuyển mô hình sang GPU để tăng tốc
    import torch
    device = "cuda" if torch.cuda.is_available() else "cpu"
    model.to(device)
    
    print(f"Mô hình {model_name} đã được tải thành công trên {device}.")
    
except Exception as e:
    print(f"Lỗi khi tải mô hình {model_name}: {e}")
    # Xử lý lỗi nếu không thể tải mô hình (ví dụ: không có kết nối internet, tên mô hình sai)
    exit(1) # Thoát ứng dụng nếu mô hình không thể tải

# Hàm để tạo chú thích từ ảnh (thay thế pipeline)
def generate_caption(image_input):
    # Tiền xử lý ảnh và chuyển đổi thành tensor
    pixel_values = feature_extractor(images=image_input, return_tensors="pt").pixel_values
    
    # Di chuyển pixel_values sang cùng thiết bị với mô hình (CPU/GPU)
    pixel_values = pixel_values.to(device)
    
    # Cấu hình tham số tạo văn bản (generation parameters)
    # max_length: độ dài tối đa của chú thích
    # num_beams: số lượng "tia" tìm kiếm để chọn câu tốt nhất (beam search)
    gen_kwargs = {"max_length": 16, "num_beams": 4} 
    
    # Tạo văn bản (chú thích)
    output_ids = model.generate(pixel_values, **gen_kwargs)
    
    # Giải mã các ID token thành văn bản
    preds = tokenizer.batch_decode(output_ids, skip_special_tokens=True)
    
    # Loại bỏ khoảng trắng thừa
    caption_text = preds[0].strip()
    
    return caption_text


@app.route('/caption', methods=['POST'])
def get_image_caption():
    data = request.get_json()
    image_url = data.get('imageUrl')

    if not image_url:
        return jsonify({'error': 'imageUrl is required'}), 400

    try:
        # Tải ảnh từ URL
        response = requests.get(image_url, stream=True)
        response.raise_for_status() # Ném lỗi cho phản hồi HTTP không thành công

        # Đọc nội dung ảnh vào bộ nhớ và mở bằng Pillow
        # Đảm bảo chuyển đổi sang "RGB" để có định dạng màu chuẩn
        image = Image.open(io.BytesIO(response.content)).convert("RGB")

        # Gọi hàm tạo chú thích của chúng ta
        caption_text = generate_caption(image)

        return jsonify({'caption': caption_text})

    except requests.exceptions.RequestException as e:
        return jsonify({'error': f'Failed to download image from URL: {e}'}), 500
    except Image.UnidentifiedImageError:
        return jsonify({'error': 'The provided URL does not point to a valid image.'}), 400
    except Exception as e:
        return jsonify({'error': f'An unexpected error occurred: {e}'}), 500

if __name__ == '__main__':
    # Chạy Flask app. Trong môi trường dev, bạn có thể dùng debug=True.
    # Trong môi trường sản phẩm, hãy dùng một WSGI server như Gunicorn hoặc uWSGI.
    # Đảm bảo cổng này không trùng với ứng dụng ASP.NET của bạn.
    app.run(host='0.0.0.0', port=5000, debug=True,use_reloader=False)