using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Ats
{
    public partial class Form1 : Form
    {
        int g_scr_no = 0; // Open API 요청번호

        string g_user_id = null;
        string g_accnt_no = null;

        public Form1()
        {
            InitializeComponent();

        }

        #region 필수 메소드 구현

        /// <summary>
        /// 현재 시각 가져오기
        /// </summary>
        /// <returns>시분초</returns>
        public string get_cur_tm()
        {
            DateTime l_cur_time;
            string l_cur_tm;

            l_cur_time = DateTime.Now;
            l_cur_tm = l_cur_time.ToString("HHmmss");

            return l_cur_tm;
        }

        /// <summary>
        /// 종목명 가져오기
        /// </summary>
        /// <param name="i_jongmok_cd">종목 코드</param>
        /// <returns>종목명</returns>
        public string get_jongmok_nm(string i_jongmok_cd)
        {
            string l_jongmok_nm = null;

            l_jongmok_nm = axKHOpenAPI1.GetMasterCodeName(i_jongmok_cd); // 종목명 가져오기

            return l_jongmok_nm;
        }

        /// <summary>
        /// 오라클 접속 연결
        /// </summary>
        /// <returns>오라클 연결 변수</returns>
        private OracleConnection connect_db()
        {
            string conninfo = "User Id=ats;Password=1234;";
            conninfo += "Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=localhost)(PORT=1521))";
            conninfo += "(CONNECTION_DATA=(SERVER=DEDICATED)(SERVICE_NAME=xe)));";

            OracleConnection conn = new OracleConnection(conninfo); // 오라클 연결 인스턴스 생성

            try
            {
                conn.Open();
            }
            catch(Exception ex)
            {
                MessageBox.Show($"connect_db() FAIL! {ex.Message} 오류 발생");
                conn = null;
            }
            return conn;
        }

        /// <summary>
        /// 메시지 로그 출력
        /// </summary>
        /// <param name="text"></param>
        /// <param name="is_clear"></param>
        public void write_msg_log(string text, int is_clear)
        {
            DateTime l_cur_time;
            string l_cur_dt;
            string l_cur_tm;
            string l_cur_dtm;

            l_cur_dt = "";
            l_cur_tm = "";

            l_cur_time = DateTime.Now;
            l_cur_dt = l_cur_time.ToString("yyyy-MM-dd");
            l_cur_tm = l_cur_time.ToString("HH:mm:ss");

            l_cur_dtm = $"[{l_cur_dt} {l_cur_tm}]";

            if(is_clear == 1)
            {
                //if (this.textBox_msg_log.InvokeRequired)
                //    textBox_msg_log.BeginInvoke(new Action(() => textBox_msg_log.Clear()));
                //else
                //    this.textBox_msg_log.Clear();

                if (this.textBox_msg_log.InvokeRequired)
                    textBox1.BeginInvoke(new Action(() => textBox_msg_log.Clear()));
                else
                    this.textBox1.Clear();
            }
            else
            {
                if (this.textBox_msg_log.InvokeRequired)
                    textBox_msg_log.BeginInvoke(new Action(() => textBox_msg_log.AppendText(l_cur_dtm + text)));
                else
                    this.textBox_msg_log.AppendText(l_cur_dtm + text);
            }
        }

        /// <summary>
        /// 에러 로그 출력
        /// </summary>
        /// <param name="text"></param>
        /// <param name="is_clear"></param>
        public void write_err_log(string text, int is_clear)
        {
            DateTime l_cur_time;
            string l_cur_dt;
            string l_cur_tm;
            string l_cur_dtm;

            l_cur_dt = "";
            l_cur_tm = "";

            l_cur_time = DateTime.Now;
            l_cur_dt = l_cur_time.ToString("yyyy-MM-dd");
            l_cur_tm = l_cur_time.ToString("HH:mm:ss");

            l_cur_dtm = $"[{l_cur_dt} {l_cur_tm}]";

            if(is_clear == 1)
            {
                if (this.textBox_err_log.InvokeRequired)
                    textBox_err_log.BeginInvoke(new Action(() => textBox_err_log.Clear()));
                else
                    textBox_err_log.Clear();
            }
            else
            {
                if (this.textBox_err_log.InvokeRequired)
                    textBox_err_log.BeginInvoke(new Action(() => textBox_err_log.AppendText(l_cur_dtm + text)));
                else
                    this.textBox_err_log.AppendText(l_cur_dtm + text);
            }
        }

        /// <summary>
        /// 지연 메서드
        /// </summary>
        /// <param name="MS"></param>
        /// <returns></returns>
        [HandleProcessCorruptedStateExceptions]
        [SecurityCritical]
        public DateTime delay(int MS)
        {
            DateTime ThisMoment = DateTime.Now;
            TimeSpan duration = new TimeSpan(0, 0, 0, 0, MS);
            DateTime AfterWards = ThisMoment.Add(duration);

            while(AfterWards >= ThisMoment)
            {
                try
                {
                    unsafe
                    {
                        Application.DoEvents();
                    }
                }
                catch(AccessViolationException ex)
                {
                    write_err_log($"delay() ex.Message : [{ex.Message}]", 0);
                }
                ThisMoment = DateTime.Now;
            }
            return DateTime.Now;
        }

        /// <summary>
        /// 요청 번호 부여 메소드
        /// </summary>
        /// <returns></returns>
        private string get_scr_no()
        {
            if (g_scr_no < 9999)
                g_scr_no++;
            else
                g_scr_no = 1000;

            return g_scr_no.ToString();
        }

        #endregion

        #region 로그인 구현

        private void 로그인ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            int ret = 0;
            int ret2 = 0;

            string l_accno = null; // 증권 계좌 번호
            string l_accno_cnt = null; // 소유한 증권계좌번호의 수
            string[] l_accno_arr = null; // N개의 증권계좌번호를 저장할 배열

            ret = axKHOpenAPI1.CommConnect(); // 로그인 창 호출

            if(ret == 0)
            {
                toolStripStatusLabel1.Text = "로그인 중...";

                for (;;)
                {
                    ret2 = axKHOpenAPI1.GetConnectState();
                    if (ret2 == 1) // 로그인이 완료되면
                        break;
                    else
                        delay(1000);
                }

                toolStripStatusLabel1.Text = "로그인 완료";

                g_user_id = "";
                g_user_id = axKHOpenAPI1.GetLoginInfo("USER_ID").Trim();
                textBox1.Text = g_user_id;

                l_accno_cnt = "";
                l_accno_cnt = axKHOpenAPI1.GetLoginInfo("ACCOUNT_CNT").Trim();  // 사용자의 증권계좌번호 수를 가져옴
                l_accno_arr = new string[int.Parse(l_accno_cnt)];
                l_accno = "";
                l_accno = axKHOpenAPI1.GetLoginInfo("ACCNO").Trim(); // 증권계좌번호 가져옴
                l_accno_arr = l_accno.Split(';');

                comboBox1.Items.Clear();
                comboBox1.Items.AddRange(l_accno_arr); // N개의 증권 계좌번호를  콤보박스에 저장
                comboBox1.SelectedIndex = 0;

                g_accnt_no = comboBox1.SelectedItem.ToString().Trim();
            }
        }

        /// <summary>
        /// 접속정보 그룹박스의 증권계좌정보를 저장
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            g_accnt_no = comboBox1.SelectedItem.ToString().Trim();
            write_msg_log($"사용할 증권계좌번호는 : [{g_accnt_no}]입니다.\n", 0);
        }

        /// <summary>
        /// 로그아웃
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void 로그아웃ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            axKHOpenAPI1.CommTerminate();
            toolStripStatusLabel1.Text = "로그아웃이 완료되었습니다";
        }

        #endregion


    }
}
