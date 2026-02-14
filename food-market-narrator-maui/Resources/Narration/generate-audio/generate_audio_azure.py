"""
Script tạo audio sử dụng Azure Speech Services
Giọng Việt tốt hơn OpenAI TTS, phù hợp cho tiếng Việt

Cách sử dụng:
    python generate_audio_azure.py --key YOUR_AZURE_KEY --region YOUR_REGION
    
    Hoặc đặt biến môi trường:
    set AZURE_SPEECH_KEY=your-key-here
    set AZURE_SPEECH_REGION=southeastasia
    python generate_audio_azure.py
"""

import os
import sys
from pathlib import Path
import azure.cognitiveservices.speech as speechsdk
import re

# Cấu hình
SCRIPT_DIR = Path(__file__).parent.parent / "scripts"
AUDIO_DIR = Path(__file__).parent.parent / "audio" / "languages"

# Cấu hình giọng nói Azure cho từng ngôn ngữ
# Xem thêm: https://learn.microsoft.com/en-us/azure/ai-services/speech-service/language-support
VOICE_CONFIG = {
    'vie': {
        'voice': 'vi-VN-NamMinhNeural',  # Giọng nam Việt tự nhiên, trầm ấm
        'language': 'vi-VN',
        'style': 'gentle',               # cheerful, friendly, gentle
        'rate': '-5%'                    # -50% to +100%, chậm hơn 1 chút để tự nhiên
    },
    'eng': {
        'voice': 'en-US-JennyNeural',    # Giọng nữ Mỹ
        'language': 'en-US',
        'style': 'friendly',
        'rate': '+0%'
    },
    'jap': {
        'voice': 'ja-JP-NanamiNeural',   # Giọng nữ Nhật
        'language': 'ja-JP',
        'style': 'cheerful',
        'rate': '+0%'
    },
    'kor': {
        'voice': 'ko-KR-SunHiNeural',    # Giọng nữ Hàn
        'language': 'ko-KR',
        'style': 'cheerful',
        'rate': '+0%'
    }
}

# Import translations từ file generate_audio.py
# (Copy phần TRANSLATIONS từ file kia, hoặc import)
TRANSLATIONS = {
    'vie': {},  # Đọc từ markdown
    'eng': {
        'lang-restaurant': """Lang Restaurant

Lang Restaurant combines the style of both a restaurant and eatery, creating a more refined atmosphere.
The space is neat, clean, and suitable for diners who want to eat comfortably.
The menu focuses on seafood, drinking dishes, and grilled items prepared elaborately.
Lang Restaurant is a good choice for those who want to experience Vinh Khanh cuisine in an elegant style.""",
        
        'alo-quan-beer-seafood': """Alo Quan Beer & Seafood

Alo Quan specializes in beer and seafood, a popular stop for food lovers in Vinh Khanh.
The spacious seating area is suitable for groups and gatherings.
The menu features fresh seafood, especially shellfish and grilled dishes.
This is a lively spot perfect for evening drinks and casual dining.""",
        
        'chilli-bbq-hotpot-restaurant': """Chilli BBQ & Hotpot Restaurant

Chilli Restaurant offers a diverse menu of grilled and hotpot dishes.
The modern, airy space is suitable for families and groups of friends.
Diners can choose from various buffet packages or order à la carte.
Chilli is ideal for those who enjoy both grilling and hotpot in one place.""",
        
        'the-gioi-bo': """Beef World

Beef World specializes in beef dishes, from grilled beef to hotpot.
High-quality beef from various sources, carefully prepared.
The restaurant space is clean and comfortable.
This is the perfect destination for beef lovers.""",
        
        'them-nuong-yakiniku': """Them Nuong Yakiniku

Them Nuong Yakiniku brings Japanese-style grilling to Vinh Khanh.
High-quality meats marinated with special recipes.
The modern space with individual grills at each table.
Suitable for those who enjoy authentic yakiniku experience.""",
        
        'lau-nuong-thuan-viet': """Lau Nuong Thuan Viet

Thuan Viet offers both hotpot and grilling with a traditional Vietnamese style.
Diverse menu with fresh ingredients.
Spacious, airy setting suitable for groups.
Good choice for family gatherings and celebrations.""",
        
        'lau-met-nuong-79k': """Lau Met Nuong 79K

A budget-friendly hotpot and grill buffet option.
Starting from 79,000 Vietnamese dong for unlimited hotpot and grilling.
Simple space but clean and comfortable.
Ideal for students and those looking for affordable dining.""",
        
        'quan-oc-vu': """Quan Oc Vu

Quan Oc Vu is one of the famous and busy shellfish restaurants on Vinh Khanh street.
The restaurant uses fresh seafood, prepares quickly, and serves continuously from evening till late night.
The atmosphere is always lively, especially on weekends.
Oc Vu contributes to the bustling street food scene characteristic of District 4.""",
        
        'oc-phat': """Oc Phat

Oc Phat is a casual shellfish eatery on the Vinh Khanh culinary street, District 4.
The restaurant specializes in fresh shellfish and seafood.
Ingredients are purchased daily and cooked on-site, ensuring freshness and natural flavors.
The simple, friendly atmosphere with reasonable prices makes Oc Phat a familiar dining spot for locals each evening.""",
        
        'quan-be-oc': """Quan Be Oc

Quan Be Oc has a casual and friendly style.
The shellfish dishes are simply prepared, maintaining freshness and natural sweetness.
Fast service, affordable prices, suitable for students and young people.
The restaurant is a familiar stop when exploring Vinh Khanh food street at night.""",
        
        'oc-oanh': """Oc Oanh

Oc Oanh offers a variety of shellfish and seafood dishes.
Fresh ingredients prepared with traditional recipes.
Cozy space suitable for groups and families.
A popular destination for shellfish lovers in Vinh Khanh.""",
        
        'oc-loan': """Oc Loan

Oc Loan specializes in shellfish with distinctive flavors.
The menu features various cooking styles from grilled to stir-fried.
Clean, airy space with enthusiastic service.
Good choice for those who love exploring street food.""",
        
        'oc-hong-nhung': """Oc Hong Nhung

Oc Hong Nhung is known for its fresh seafood and unique processing methods.
Rich menu with many special dishes.
Spacious seating area suitable for groups.
Reasonable prices and quality service.""",
        
        'oc-hoa-kieu': """Oc Hoa Kieu

Oc Hoa Kieu brings a refined style to street shellfish dining.
Fresh seafood prepared carefully to preserve natural flavors.
Clean, comfortable space.
Ideal for those seeking quality shellfish experiences.""",
        
        'oc-cuc-vinh-khanh': """Oc Cuc Vinh Khanh

Oc Cuc is a familiar name on Vinh Khanh food street.
Diverse menu from traditional to creative shellfish dishes.
Lively atmosphere, especially in the evening.
A must-visit when exploring District 4 cuisine.""",
        
        'quan-oc-thao-quan-4': """Quan Oc Thao District 4

Quan Oc Thao specializes in shellfish and late-night seafood.
Fresh ingredients with quick and skillful preparation.
Spacious area suitable for gatherings.
Popular spot for night owls in District 4.""",
    },
    'jap': {    # Bản dịch tiếng Nhật
        'lang-restaurant': """ラン・レストラン

ラン・レストランは、レストランと食堂のスタイルを組み合わせ、より洗練された雰囲気を作り出しています。
清潔で整った空間は、快適に食事をしたいお客様に適しています。
メニューはシーフード、おつまみ、丁寧に調理された焼き料理を中心としています。
ラン・レストランは、優雅なスタイルでヴィンカイン料理を体験したい方に最適な選択です。""",
        
        'alo-quan-beer-seafood': """アロー・クアン ビール＆シーフード

アロー・クアンはビールとシーフードを専門とし、ヴィンカインの食通に人気のスポットです。
広々とした座席エリアは、グループや集まりに適しています。
メニューは新鮮なシーフード、特に貝類や焼き料理が特徴です。
夕方の飲み会やカジュアルな食事に最適な活気あるスポットです。""",
        
        'chilli-bbq-hotpot-restaurant': """チリ バーベキュー＆鍋レストラン

チリレストランは、焼肉と鍋料理の多様なメニューを提供しています。
モダンで風通しの良い空間は、家族や友人グループに適しています。
お客様は様々なビュッフェパッケージを選ぶか、アラカルトで注文できます。
チリは焼肉と鍋の両方を楽しみたい方に理想的です。""",
        
        'the-gioi-bo': """ザ・ジョイ・ボー（ビーフワールド）

ザ・ジョイ・ボーは、焼き牛肉から鍋まで、牛肉料理を専門としています。
様々な産地の高品質な牛肉を丁寧に調理しています。
レストランの空間は清潔で快適です。
牛肉愛好家にとって完璧な目的地です。""",
        
        'them-nuong-yakiniku': """テムヌォン 焼肉

テムヌォン焼肉は、日本スタイルの焼肉をヴィンカインに提供しています。
特別なレシピでマリネされた高品質な肉。
各テーブルに個別のグリルがあるモダンな空間。
本格的な焼肉体験を楽しみたい方に適しています。""",
        
        'lau-nuong-thuan-viet': """ラウヌォン トゥアンヴィエット

トゥアンヴィエットは、伝統的なベトナムスタイルの鍋と焼肉の両方を提供しています。
新鮮な食材を使用した多様なメニュー。
グループに適した広々とした空間。
家族の集まりやお祝いに最適な選択です。""",
        
        'lau-met-nuong-79k': """ラウメット ヌォン 79K

お手頃価格の鍋と焼肉のビュッフェオプション。
79,000ベトナムドンから無制限の鍋と焼肉。
シンプルだが清潔で快適な空間。
学生やお手頃な食事を探している方に理想的です。""",
        
        'quan-oc-vu': """クアン・オック・ヴー

クアン・オック・ヴーは、ヴィンカイン通りで有名で賑わう貝料理店の一つです。
レストランは新鮮なシーフードを使用し、迅速に調理し、夕方から深夜まで継続的にサービスを提供しています。
雰囲気は常に活気があり、特に週末は賑わっています。
オック・ヴーは、第4区特有の賑やかなストリートフードシーンに貢献しています。""",
        
        'oc-phat': """オック・ファット

オック・ファットは、第4区のヴィンカイン料理通りにあるカジュアルな貝料理店です。
レストランは、巻貝、あさり、牡蠣、イカ、エビなどの新鮮な貝類とシーフードを専門としています。
食材は毎日購入され、店内で調理されるため、新鮮さと自然な風味が保証されています。
シンプルで親しみやすい雰囲気とリーズナブルな価格により、オック・ファットは地元の人々が毎晩訪れる馴染みの食事スポットとなっています。""",
        
        'quan-be-oc': """クアン・ベー・オック

クアン・ベー・オックは、カジュアルで親しみやすいスタイルです。
貝料理はシンプルに調理され、新鮮さと自然な甘みを保っています。
迅速なサービス、お手頃な価格で、学生や若者に適しています。
夜のヴィンカイン フードストリート探索時の馴染みの立ち寄りスポットです。""",
        
        'oc-oanh': """オック・オアイン

オック・オアインは、様々な貝類とシーフード料理を提供しています。
伝統的なレシピで調理された新鮮な食材。
グループや家族に適した居心地の良い空間。
ヴィンカインの貝類愛好家に人気の目的地です。""",
        
        'oc-loan': """オック・ロアン

オック・ロアンは、独特の風味を持つ貝類を専門としています。
メニューは、焼きから炒めまで様々な調理スタイルを特徴としています。
清潔で風通しの良い空間、熱心なサービス。
ストリートフード探索が好きな方に良い選択です。""",
        
        'oc-hong-nhung': """オック・ホン・ニュン

オック・ホン・ニュンは、新鮮なシーフードとユニークな調理方法で知られています。
多くの特別料理を含む豊富なメニュー。
グループに適した広々とした座席エリア。
リーズナブルな価格と質の高いサービス。""",
        
        'oc-hoa-kieu': """オック・ホア・キエウ

オック・ホア・キエウは、ストリート貝料理に洗練されたスタイルをもたらします。
新鮮なシーフードを丁寧に調理し、自然な風味を保存しています。
清潔で快適な空間。
質の高い貝類体験を求める方に理想的です。""",
        
        'oc-cuc-vinh-khanh': """オック・クック ヴィンカイン

オック・クックは、ヴィンカイン フードストリートで馴染みの名前です。
伝統的から創造的な貝料理まで多様なメニュー。
活気ある雰囲気、特に夕方。
第4区料理を探索する際の必訪スポットです。""",
        
        'quan-oc-thao-quan-4': """クアン・オック・タオ 第4区

クアン・オック・タオは、貝類と深夜シーフードを専門としています。
迅速で熟練した調理による新鮮な食材。
集まりに適した広々としたエリア。
第4区の夜更かしに人気のスポットです。""",
    },
    'kor': {    # Bản dịch tiếng Hàn
        'lang-restaurant': """랑 레스토랑

랑 레스토랑은 식당과 레스토랑 스타일을 결합하여 더욱 세련된 분위기를 조성합니다.
깔끔하고 청결한 공간은 편안하게 식사하고 싶은 손님들에게 적합합니다.
메뉴는 해산물, 안주 요리, 정성스럽게 조리된 구이 요리에 중점을 둡니다.
랑 레스토랑은 우아한 스타일로 빈칸 요리를 경험하고 싶은 분들에게 좋은 선택입니다.""",
        
        'alo-quan-beer-seafood': """알로 콴 맥주 & 해산물

알로 콴은 맥주와 해산물을 전문으로 하며, 빈칸의 음식 애호가들에게 인기 있는 장소입니다.
넓은 좌석 공간은 단체 및 모임에 적합합니다.
메뉴는 신선한 해산물, 특히 조개류와 구이 요리가 특징입니다.
저녁 술자리와 캐주얼한 식사에 완벽한 활기찬 장소입니다.""",
        
        'chilli-bbq-hotpot-restaurant': """칠리 바비큐 & 핫팟 레스토랑

칠리 레스토랑은 구이와 전골 요리의 다양한 메뉴를 제공합니다.
현대적이고 통풍이 잘 되는 공간은 가족과 친구 그룹에 적합합니다.
손님들은 다양한 뷔페 패키지를 선택하거나 단품으로 주문할 수 있습니다.
칠리는 구이와 전골을 모두 즐기는 분들에게 이상적입니다.""",
        
        'the-gioi-bo': """더 지오이 보 (비프 월드)

더 지오이 보는 구운 소고기부터 전골까지 소고기 요리를 전문으로 합니다.
다양한 출처의 고품질 소고기를 정성스럽게 준비합니다.
레스토랑 공간은 깨끗하고 편안합니다.
소고기 애호가들을 위한 완벽한 목적지입니다.""",
        
        'them-nuong-yakiniku': """템 누옹 야키니쿠

템 누옹 야키니쿠는 일본식 구이를 빈칸에 제공합니다.
특별한 레시피로 양념된 고품질 고기.
각 테이블에 개별 그릴이 있는 현대적인 공간.
정통 야키니쿠 경험을 즐기고 싶은 분들에게 적합합니다.""",
        
        'lau-nuong-thuan-viet': """라우 누옹 투안 비엣

투안 비엣은 전통적인 베트남 스타일의 전골과 구이를 모두 제공합니다.
신선한 재료를 사용한 다양한 메뉴.
그룹에 적합한 넓고 통풍이 잘 되는 공간.
가족 모임 및 축하 행사에 좋은 선택입니다.""",
        
        'lau-met-nuong-79k': """라우 멧 누옹 79K

저렴한 전골 및 구이 뷔페 옵션.
79,000 동부터 무제한 전골 및 구이.
간단하지만 깨끗하고 편안한 공간.
학생 및 저렴한 식사를 찾는 분들에게 이상적입니다.""",
        
        'quan-oc-vu': """콴 옥 부

콴 옥 부는 빈칸 거리에서 유명하고 붐비는 조개 레스토랑 중 하나입니다.
레스토랑은 신선한 해산물을 사용하고 빠르게 준비하며 저녁부터 심야까지 지속적으로 서비스를 제공합니다.
분위기는 항상 활기차며 특히 주말에 그렇습니다.
옥 부는 4군 특유의 활기찬 길거리 음식 장면에 기여합니다.""",
        
        'oc-phat': """옥 팟

옥 팟은 4군 빈칸 요리 거리의 캐주얼한 조개 식당입니다.
레스토랑은 달팽이, 조개, 굴, 오징어, 새우와 같은 신선한 조개류와 해산물을 전문으로 합니다.
재료는 매일 구매되고 현장에서 조리되어 신선도와 자연스러운 맛을 보장합니다.
간단하고 친근한 분위기와 합리적인 가격으로 옥 팟은 매일 저녁 현지인들이 찾는 익숙한 식사 장소가 되었습니다.""",
        
        'quan-be-oc': """콴 베 옥

콴 베 옥은 캐주얼하고 친근한 스타일입니다.
조개 요리는 간단하게 준비되어 신선도와 자연스러운 단맛을 유지합니다.
빠른 서비스, 저렴한 가격으로 학생과 젊은이들에게 적합합니다.
밤에 빈칸 푸드 스트리트를 탐험할 때 익숙한 정류장입니다.""",
        
        'oc-oanh': """옥 오아인

옥 오아인은 다양한 조개류와 해산물 요리를 제공합니다.
전통 레시피로 조리된 신선한 재료.
그룹과 가족에게 적합한 아늑한 공간.
빈칸의 조개류 애호가들에게 인기 있는 목적지입니다.""",
        
        'oc-loan': """옥 로안

옥 로안은 독특한 맛을 가진 조개류를 전문으로 합니다.
메뉴는 구이부터 볶음까지 다양한 요리 스타일을 특징으로 합니다.
깨끗하고 통풍이 잘 되는 공간, 열정적인 서비스.
길거리 음식 탐험을 좋아하는 분들에게 좋은 선택입니다.""",
        
        'oc-hong-nhung': """옥 홍 눙

옥 홍 눙은 신선한 해산물과 독특한 가공 방법으로 유명합니다.
많은 특별 요리를 포함한 풍부한 메뉴.
그룹에 적합한 넓은 좌석 공간.
합리적인 가격과 양질의 서비스.""",
        
        'oc-hoa-kieu': """옥 호아 키에우

옥 호아 키에우는 길거리 조개 식사에 세련된 스타일을 제공합니다.
신선한 해산물을 신중하게 준비하여 자연스러운 맛을 보존합니다.
깨끗하고 편안한 공간.
양질의 조개류 경험을 찾는 분들에게 이상적입니다.""",
        
        'oc-cuc-vinh-khanh': """옥 컥 빈칸

옥 컥은 빈칸 푸드 스트리트에서 익숙한 이름입니다.
전통적인 것부터 창의적인 조개 요리까지 다양한 메뉴.
특히 저녁에 활기찬 분위기.
4군 요리를 탐험할 때 꼭 방문해야 할 곳입니다.""",
        
        'quan-oc-thao-quan-4': """콴 옥 타오 4군

콴 옥 타오는 조개류와 심야 해산물을 전문으로 합니다.
빠르고 숙련된 준비로 신선한 재료.
모임에 적합한 넓은 공간.
4군의 올빼미족에게 인기 있는 장소입니다.""",
    }
}


def clean_text(text):
    """Làm sạch text cho TTS"""
    text = re.sub(r'🎤\s*', '', text)
    text = re.sub(r'\s+', ' ', text)
    text = text.strip()
    return text


def escape_xml(text):
    """Escape các ký tự đặc biệt XML/HTML cho SSML"""
    text = text.replace('&', '&amp;')
    text = text.replace('<', '&lt;')
    text = text.replace('>', '&gt;')
    text = text.replace('"', '&quot;')
    text = text.replace("'", '&apos;')
    return text


def read_script(script_path):
    """Đọc script từ markdown"""
    with open(script_path, 'r', encoding='utf-8') as f:
        content = f.read()
    return clean_text(content)


def create_ssml(text, language, config):
    """Tạo SSML cho Azure TTS với style và rate"""
    voice = config['voice']
    style = config.get('style', 'friendly')
    rate = config.get('rate', '+0%')
    
    # Escape các ký tự đặc biệt XML
    text_escaped = escape_xml(text)
    
    ssml = f"""<speak version="1.0" xmlns="http://www.w3.org/2001/10/synthesis" 
               xmlns:mstts="https://www.w3.org/2001/mstts" xml:lang="{language}">
        <voice name="{voice}">
            <mstts:express-as style="{style}">
                <prosody rate="{rate}">
                    {text_escaped}
                </prosody>
            </mstts:express-as>
        </voice>
    </speak>"""
    return ssml


def generate_audio(speech_config, text, language, output_path):
    """Tạo audio sử dụng Azure Speech"""
    config = VOICE_CONFIG.get(language, VOICE_CONFIG['vie'])
    
    try:
        print(f"  Generating: {output_path.name}")
        
        # Cấu hình audio output
        audio_config = speechsdk.audio.AudioOutputConfig(filename=str(output_path))
        
        # Tạo synthesizer
        speech_config.speech_synthesis_voice_name = config['voice']
        synthesizer = speechsdk.SpeechSynthesizer(
            speech_config=speech_config, 
            audio_config=audio_config
        )
        
        # Tạo SSML
        ssml = create_ssml(text, config['language'], config)
        
        # Synthesize
        result = synthesizer.speak_ssml_async(ssml).get()
        
        if result.reason == speechsdk.ResultReason.SynthesizingAudioCompleted:
            print(f"  ✓ Success: {output_path.name}")
            return True
        elif result.reason == speechsdk.ResultReason.Canceled:
            cancellation = result.cancellation_details
            print(f"  ✗ Error: {cancellation.reason}")
            if cancellation.error_details:
                print(f"     Details: {cancellation.error_details}")
            return False
        
    except Exception as e:
        print(f"  ✗ Exception: {str(e)}")
        return False


def get_translated_text(script_name, language):
    """Lấy text cho ngôn ngữ"""
    base_name = script_name.replace('.md', '')
    
    if language == 'vie':
        script_path = SCRIPT_DIR / script_name
        if script_path.exists():
            return read_script(script_path)
    else:
        return TRANSLATIONS.get(language, {}).get(base_name)
    
    return None


def main():
    # Lấy API key và region
    speech_key = os.environ.get('AZURE_SPEECH_KEY')
    speech_region = os.environ.get('AZURE_SPEECH_REGION', 'southeastasia')
    
    # Parse arguments
    if len(sys.argv) > 2:
        if sys.argv[1] == '--key':
            speech_key = sys.argv[2]
        if len(sys.argv) > 4 and sys.argv[3] == '--region':
            speech_region = sys.argv[4]
    
    if not speech_key:
        print("Error: AZURE_SPEECH_KEY not found!")
        print("\nUsage:")
        print("  Option 1: Set environment variables")
        print("    set AZURE_SPEECH_KEY=your-key-here")
        print("    set AZURE_SPEECH_REGION=southeastasia")
        print("    python generate_audio_azure.py")
        print("\n  Option 2: Pass as arguments")
        print("    python generate_audio_azure.py --key your-key --region southeastasia")
        sys.exit(1)
    
    # Tạo speech config
    speech_config = speechsdk.SpeechConfig(
        subscription=speech_key,
        region=speech_region
    )
    speech_config.set_speech_synthesis_output_format(
        speechsdk.SpeechSynthesisOutputFormat.Audio16Khz32KBitRateMonoMp3
    )
    
    # Lấy danh sách scripts
    script_files = list(SCRIPT_DIR.glob("*.md"))
    if not script_files:
        print(f"No script files found in {SCRIPT_DIR}")
        sys.exit(1)
    
    print(f"Found {len(script_files)} script files")
    print(f"Region: {speech_region}")
    print(f"\nGenerating audio for languages: vie, eng, jap, kor\n")
    
    total_success = 0
    total_failed = 0
    
    # Process each script
    for script_file in script_files:
        script_name = script_file.name
        base_name = script_name.replace('.md', '')
        
        print(f"\n{'='*60}")
        print(f"Processing: {script_name}")
        print(f"{'='*60}")
        
        for lang_code in ['vie', 'eng', 'jap', 'kor']:
            print(f"\n[{lang_code.upper()}]")
            
            text = get_translated_text(script_name, lang_code)
            
            if not text:
                print(f"  ⚠ Skipping: No translation available")
                continue
            
            output_dir = AUDIO_DIR / lang_code
            output_dir.mkdir(parents=True, exist_ok=True)
            
            output_file = output_dir / f"{base_name}.mp3"
            
            if generate_audio(speech_config, text, lang_code, output_file):
                total_success += 1
            else:
                total_failed += 1
    
    print(f"\n{'='*60}")
    print(f"SUMMARY")
    print(f"{'='*60}")
    print(f"✓ Success: {total_success} files")
    print(f"✗ Failed:  {total_failed} files")
    print(f"\nAudio files saved to: {AUDIO_DIR}")


if __name__ == "__main__":
    main()
